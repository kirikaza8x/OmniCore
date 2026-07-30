namespace OmniCore.Shared.Infrastructure.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.Pagination;
using OmniCore.Shared.Domain.Queries;

public static class QueryableExtensions
{
    private static readonly IDictionary<string, string> Operators = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"eq", "="}, {"neq", "!="}, {"lt", "<"}, {"lte", "<="},
        {"gt", ">"}, {"gte", ">="},
        {"startswith", "StartsWith"}, {"endswith", "EndsWith"},
        {"contains", "Contains"}, {"doesnotcontain", "Contains"}
    };

    #region Pagination
    public static IQueryable<TEntity> ApplyPagination<TEntity>(
        this IQueryable<TEntity> query, IPageable pageable)
    {
        int page = pageable.PageNumber > 0 ? pageable.PageNumber : 1;
        int size = pageable.PageSize > 0 ? pageable.PageSize : 10;
        return System.Linq.Queryable.Skip(query, (page - 1) * size).Take(size);
    }

    public static async Task<DomainPagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int validPage = pageNumber > 0 ? pageNumber : 1;
        int validSize = pageSize > 0 ? pageSize : 10;

        int totalCount = await query.CountAsync(cancellationToken);
        List<T> items = await System.Linq.Queryable.Skip(query, (validPage - 1) * validSize)
            .Take(validSize)
            .ToListAsync(cancellationToken);

        return DomainPagedResult<T>.Create(items, validPage, validSize, totalCount);
    }

    public static async Task<DomainPagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(pagedQuery.SortColumn))
        {
            query = query.ApplyDynamicSorting(new[] { new Sort(pagedQuery.SortColumn, pagedQuery.SortOrder) });
        }

        return await query.ToPagedResultAsync(
            pagedQuery.PageNumber,
            pagedQuery.PageSize,
            cancellationToken);
    }

    public static async Task<DomainPagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        IPageable pageable,
        CancellationToken cancellationToken = default)
    {
        int totalCount = await query.CountAsync(cancellationToken);
        List<T> items = await query.ApplyPagination(pageable).ToListAsync(cancellationToken);

        return DomainPagedResult<T>.Create(
            items,
            pageable.PageNumber > 0 ? pageable.PageNumber : 1,
            pageable.PageSize > 0 ? pageable.PageSize : 10,
            totalCount);
    }
    #endregion

    #region Dynamic Filtering
    public static IQueryable<T> ApplyDynamicFilters<T>(this IQueryable<T> query, Filter? filter)
    {
        if (filter is null) return query;

        bool hasChildFilters = filter.Filters != null && filter.Filters.Any();
        bool hasValidCurrentFilter = !string.IsNullOrWhiteSpace(filter.Field) &&
                                     !string.IsNullOrWhiteSpace(filter.Operator) &&
                                     Operators.ContainsKey(filter.Operator);

        if (hasChildFilters || hasValidCurrentFilter)
        {
            var filters = GetAllFilters(filter);
            if (!filters.Any()) return query;

            var values = filters.Select(f => ExtractValue(f.Value)).ToArray();
            var where = Transform(filter, filters);

            if (string.IsNullOrWhiteSpace(where) || where == "()") return query;

            return query.Where(where, values);
        }

        return query;
    }

    private static IList<Filter> GetAllFilters(Filter filter)
    {
        var filters = new List<Filter>();
        GetFilters(filter, filters);
        return filters;
    }

    private static void GetFilters(Filter filter, IList<Filter> filters)
    {
        if (filter.Filters != null && filter.Filters.Any())
        {
            foreach (var item in filter.Filters) GetFilters(item, filters);
        }
        else if (!string.IsNullOrWhiteSpace(filter.Field) && !string.IsNullOrWhiteSpace(filter.Operator))
        {
            filters.Add(filter);
        }
    }

    private static string Transform(Filter filter, IList<Filter> filters)
    {
        if (filter.Filters != null && filter.Filters.Any())
        {
            var children = filter.Filters
                .Select(f => Transform(f, filters))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            return children.Length > 0 ? $"({string.Join($" {filter.Logic} ", children)})" : "";
        }

        int index = filters.IndexOf(filter);
        if (index < 0) return "";

        if (string.IsNullOrWhiteSpace(filter.Operator) || !Operators.TryGetValue(filter.Operator, out var comparison))
        {
            return "";
        }

        if (filter.Operator.Equals("doesnotcontain", StringComparison.OrdinalIgnoreCase))
        {
            return $"(!{filter.Field}.Contains(@{index}))";
        }

        if (comparison is "StartsWith" or "EndsWith" or "Contains")
        {
            return $"({filter.Field}.{comparison}(@{index}))";
        }

        return $"{filter.Field} {comparison} @{index}";
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
    public static IQueryable<T> ApplyDynamicSorting<T>(this IQueryable<T> query, IEnumerable<Sort>? sorts)
    {
        if (sorts is null) return query;

        var validSorts = sorts
            .Where(s => !string.IsNullOrWhiteSpace(s.Field))
            .ToList();

        if (!validSorts.Any()) return query;

        var sortExpressions = validSorts.Select(s =>
        {
            string sortDir = s.Order == SortOrder.Descending ? "desc" : "asc";
            return $"{s.Field} {sortDir}";
        });

        string ordering = string.Join(", ", sortExpressions);
        return !string.IsNullOrWhiteSpace(ordering) ? query.OrderBy(ordering) : query;
    }
    #endregion
}