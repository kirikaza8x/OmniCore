namespace OmniCore.Shared.Infrastructure.Specifications;

using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.Specifications;

/// <summary>
/// Evaluates a specification against an <see cref="IQueryable{T}"/> to apply filtering, 
/// inclusions, ordering, tracking preferences, and pagination at the database query level.
/// </summary>

public static class SpecificationEvaluator<T> where T : class
{
    /// <summary>
    /// Applies specification criteria, includes, ordering, and paging to the input queryable.
    /// </summary>
    /// <param name="inputQuery">The base <see cref="IQueryable{T}"/> target (e.g., DbSet).</param>
    /// <param name="specification">The domain specification defining query rules.</param>
    /// <returns>A modified <see cref="IQueryable{T}"/> ready for execution.</returns>
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T>? specification)
    {
        if (specification is null) return inputQuery;

        IQueryable<T> query = inputQuery;

        // 1. AsNoTracking optimization
        if (specification.IsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // 2. Filter Criteria
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        // 3. Strongly-typed Expression Includes
        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

        // 4. String-based Navigation Includes
        query = specification.IncludeStrings.Aggregate(
            query,
            (current, include) => current.Include(include));

        // 5. Ordering
        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // 6. Pagination
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}