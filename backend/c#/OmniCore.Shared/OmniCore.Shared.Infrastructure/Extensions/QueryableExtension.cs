namespace OmniCore.Shared.Infrastructure.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.Pagination;
using OmniCore.Shared.Domain.Queries;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> providing dynamic filtering, dynamic sorting, and pagination capabilities.
/// </summary>
public static class QueryableExtensions
{
    private static readonly IDictionary<string, string> Operators = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "eq", "=" }, { "neq", "!=" }, { "lt", "<" }, { "lte", "<=" },
        { "gt", ">" }, { "gte", ">=" },
        { "startswith", "StartsWith" }, { "endswith", "EndsWith" },
        { "contains", "Contains" }, { "doesnotcontain", "Contains" }
    };

    /// <summary>Largest page a caller can ask for (matches <see cref="PagedQuery"/>); bigger values are clamped.</summary>
    public const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    // Page and size from callers that did not go through PagedQuery: clamp, and compute the offset in 64 bits
    // so a huge page number cannot overflow into a negative Skip.
    internal static (int Page, int Size, int Skip) NormalizePaging(int pageNumber, int pageSize)
    {
        int page = pageNumber > 0 ? pageNumber : 1;
        int size = pageSize > 0 ? Math.Min(pageSize, MaxPageSize) : DefaultPageSize;
        long skip = (long)(page - 1) * size;
        return (page, size, (int)Math.Min(skip, int.MaxValue));
    }

    #region Pagination

    /// <summary>
    /// Applies skip and take operators to the query according to the specified pageable parameters.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity being queried.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pageable">The pageable contract specifying page number and size.</param>
    /// <returns>The paginated queryable.</returns>
    public static IQueryable<TEntity> ApplyPagination<TEntity>(
        this IQueryable<TEntity> query, 
        IPageable pageable)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(pageable);

        var (_, size, skip) = NormalizePaging(pageable.PageNumber, pageable.PageSize);

        return query.Skip(skip).Take(size);
    }

    /// <summary>
    /// Asynchronously converts an <see cref="IQueryable{T}"/> into a <see cref="DomainPagedResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pageNumber">The target page number (1-indexed).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A paged result container holding the items and total count metadata.</returns>
    public static async Task<DomainPagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (validPage, validSize, skip) = NormalizePaging(pageNumber, pageSize);

        int totalCount = await query.CountAsync(cancellationToken);
        List<T> items = await query.Skip(skip)
                                   .Take(validSize)
                                   .ToListAsync(cancellationToken);

        return DomainPagedResult<T>.Create(items, validPage, validSize, totalCount);
    }

    /// <summary>
    /// Asynchronously applies sorting and converts an <see cref="IQueryable{T}"/> into a <see cref="DomainPagedResult{T}"/> based on a <see cref="PagedQuery"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagedQuery">The paged query containing page number, size, and sort specifications.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A paged result container holding the items and total count metadata.</returns>
    public static Task<DomainPagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(pagedQuery);

        if (!string.IsNullOrWhiteSpace(pagedQuery.SortColumn))
        {
            query = query.ApplyDynamicSorting(new[] { new Sort(pagedQuery.SortColumn, pagedQuery.SortOrder) });
        }

        return query.ToPagedResultAsync(pagedQuery.PageNumber, pagedQuery.PageSize, cancellationToken);
    }

    /// <summary>
    /// Asynchronously converts an <see cref="IQueryable{T}"/> into a <see cref="DomainPagedResult{T}"/> using an <see cref="IPageable"/> specification.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pageable">The pageable specification.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A paged result container holding the items and total count metadata.</returns>
    public static Task<DomainPagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        IPageable pageable,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(pageable);

        return query.ToPagedResultAsync(pageable.PageNumber, pageable.PageSize, cancellationToken);
    }

    #endregion

    #region Dynamic Filtering

    /// <summary>
    /// Dynamically applies composite filter rules to an <see cref="IQueryable{T}"/> using System.Linq.Dynamic.Core.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="filter">The root filter tree node.</param>
    /// <returns>The filtered queryable.</returns>
    public static IQueryable<T> ApplyDynamicFilters<T>(this IQueryable<T> query, Filter? filter)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (filter is null) return query;

        var values = new List<object?>();
        CollectFilterValues(typeof(T), filter, values);

        if (values.Count == 0) return query;

        int indexPointer = 0;
        string whereClause = TransformFilter(typeof(T), filter, ref indexPointer);

        if (string.IsNullOrWhiteSpace(whereClause) || whereClause == "()") 
            return query;

        try
        {
            return query.Where(whereClause, values.ToArray());
        }
        catch (ParseException)
        {
            // Fallback safely if invalid field names or unsupported operations are passed by client requests
            return query;
        }
    }

    private static void CollectFilterValues(Type entity, Filter filter, List<object?> values)
    {
        if (filter.Filters != null && filter.Filters.Count > 0)
        {
            foreach (var child in filter.Filters)
            {
                CollectFilterValues(entity, child, values);
            }
        }
        else if (IsValidLeafFilter(entity, filter))
        {
            values.Add(ExtractValue(filter.Value));
        }
    }

    private static string TransformFilter(Type entity, Filter filter, ref int index)
    {
        if (filter.Filters != null && filter.Filters.Count > 0)
        {
            var children = new List<string>();
            foreach (var child in filter.Filters)
            {
                var childClause = TransformFilter(entity, child, ref index);
                if (!string.IsNullOrEmpty(childClause))
                {
                    children.Add(childClause);
                }
            }

            return children.Count > 0 
                ? $"({string.Join($" {(string.Equals(filter.Logic, "or", StringComparison.OrdinalIgnoreCase) ? "or" : "and")} ", children)})" 
                : string.Empty;
        }

        if (!IsValidLeafFilter(entity, filter)) return string.Empty;

        Operators.TryGetValue(filter.Operator!, out var comparison);
        int currentIndex = index++;

        if (filter.Operator!.Equals("doesnotcontain", StringComparison.OrdinalIgnoreCase))
        {
            return $"(!{filter.Field}.Contains(@{currentIndex}))";
        }

        if (comparison is "StartsWith" or "EndsWith" or "Contains")
        {
            return $"({filter.Field}.{comparison}(@{currentIndex}))";
        }

        return $"{filter.Field} {comparison} @{currentIndex}";
    }

    private static bool IsValidLeafFilter(Type entity, Filter filter)
    {
        return IsSafeFieldPath(entity, filter.Field) &&
               !string.IsNullOrWhiteSpace(filter.Operator) &&
               Operators.ContainsKey(filter.Operator);
    }

    /// <summary>
    /// A field name from a client goes into a Dynamic LINQ string, so it must be a plain property path
    /// (<c>Name</c>, <c>Address.City</c>) that exists on the entity: no method calls, operators, quotes or parentheses.
    /// Without this a client could send <c>PasswordHash.StartsWith("a") or Id</c> and read any property one character at a time.
    /// </summary>
    internal static bool IsSafeFieldPath(Type entity, string? field)
    {
        if (string.IsNullOrWhiteSpace(field) || field.Length > 100) return false;

        var current = entity;
        foreach (var segment in field.Split('.'))
        {
            if (segment.Length == 0 || !IsIdentifier(segment)) return false;

            var property = current.GetProperty(segment,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (property is null || property.GetIndexParameters().Length > 0) return false;

            current = property.PropertyType;
        }

        return true;
    }

    private static bool IsIdentifier(string value)
    {
        if (!(char.IsLetter(value[0]) || value[0] == '_')) return false;
        foreach (var c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }

    private static object? ExtractValue(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
                JsonValueKind.String => element.TryGetDateTime(out var d) ? d :
                                        element.TryGetGuid(out var g) ? g : element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => element.ToString()
            };
        }
        return value;
    }

    #endregion

    #region Dynamic Sorting

    /// <summary>
    /// Dynamically applies order specifications to an <see cref="IQueryable{T}"/> using System.Linq.Dynamic.Core.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="sorts">The list of sort field descriptors.</param>
    /// <returns>The sorted queryable.</returns>
    public static IQueryable<T> ApplyDynamicSorting<T>(this IQueryable<T> query, IEnumerable<Sort>? sorts)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (sorts is null) return query;

        var validSorts = sorts
            .Where(s => IsSafeFieldPath(typeof(T), s.Field))
            .ToList();

        if (validSorts.Count == 0) return query;

        var sortExpressions = validSorts.Select(s =>
        {
            string sortDir = s.Order == SortOrder.Descending ? "desc" : "asc";
            return $"{s.Field} {sortDir}";
        });

        string ordering = string.Join(", ", sortExpressions);
        
        try
        {
            return !string.IsNullOrWhiteSpace(ordering) ? query.OrderBy(ordering) : query;
        }
        catch (ParseException)
        {
            // Fallback safely if invalid sort fields are passed
            return query;
        }
    }

    #endregion
}