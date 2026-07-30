namespace OmniCore.Shared.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.Pagination;
using OmniCore.Shared.Domain.Queries;
using OmniCore.Shared.Domain.Specifications;
using OmniCore.Shared.Infrastructure.Extensions;

public partial class RepositoryBase<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    /// <summary>
    /// Finds an entity by its primary key identifier.
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    /// <summary>
    /// Returns the first entity matching the specification criteria, or null if no match is found.
    /// </summary>
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Checks whether an entity with the specified identifier exists in the database.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id!.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Lists all entities matching the specified specification rules.
    /// </summary>
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a paginated set of entities based on a specification and standard page request options.
    /// </summary>
    public virtual async Task<DomainPagedResult<TEntity>> GetPagedAsync(
        ISpecification<TEntity> spec, 
        PagedQuery query, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> dbQuery = ApplySpecification(spec);

        if (!string.IsNullOrWhiteSpace(query.SortColumn))
        {
            dbQuery = dbQuery.ApplyDynamicSorting([new Sort(query.SortColumn, query.SortOrder)]);
        }

        return await dbQuery.ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>
    /// Retrieves a paginated set of entities with dynamic multi-column filtering and dynamic sorting options.
    /// </summary>
    public virtual async Task<DomainPagedResult<TEntity>> GetAdvancedPagedAsync(
        ISpecification<TEntity> spec, 
        AdvancedPagedQuery query, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> dbQuery = ApplySpecification(spec);

        dbQuery = dbQuery.ApplyDynamicFilters(query.Filter);

        dbQuery = (query.Sorts is null || !query.Sorts.Any())
            ? dbQuery.ApplyDynamicSorting([new Sort("CreatedAt", SortOrder.Descending)])
            : dbQuery.ApplyDynamicSorting(query.Sorts);

        return await dbQuery.ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>
    /// Counts total records matching an optional specification rule.
    /// </summary>
    public virtual async Task<int> CountAsync(
        ISpecification<TEntity>? spec = null, 
        CancellationToken cancellationToken = default)
    {
        return spec is null
            ? await DbSet.CountAsync(cancellationToken)
            : await ApplySpecification(spec).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if any records match the specification conditions.
    /// </summary>
    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).AnyAsync(cancellationToken);
    }
}