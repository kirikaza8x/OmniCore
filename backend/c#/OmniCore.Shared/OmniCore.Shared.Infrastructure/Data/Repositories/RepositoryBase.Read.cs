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
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id!.Equals(id), cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }

    public virtual async Task<DomainPagedResult<TEntity>> GetPagedAsync(
        ISpecification<TEntity> spec, 
        PagedQuery query, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> dbQuery = ApplySpecification(spec);

        if (!string.IsNullOrWhiteSpace(query.SortColumn))
        {
            dbQuery = dbQuery.ApplyDynamicSorting(new[] { new Sort(query.SortColumn, query.SortOrder) });
        }

        return await dbQuery.ToPagedResultAsync(query, cancellationToken);
    }

    public virtual async Task<DomainPagedResult<TEntity>> GetAdvancedPagedAsync(
        ISpecification<TEntity> spec, 
        AdvancedPagedQuery query, 
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> dbQuery = ApplySpecification(spec);

        dbQuery = dbQuery.ApplyDynamicFilters(query.Filter);

        dbQuery = (query.Sorts is null || !query.Sorts.Any())
            ? dbQuery.ApplyDynamicSorting(new[] { new Sort("CreatedAt", SortOrder.Descending) })
            : dbQuery.ApplyDynamicSorting(query.Sorts);

        return await dbQuery.ToPagedResultAsync(query, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity>? spec = null, 
        CancellationToken cancellationToken = default)
    {
        return spec is null
            ? await DbSet.CountAsync(cancellationToken)
            : await ApplySpecification(spec).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).AnyAsync(cancellationToken);
    }
}