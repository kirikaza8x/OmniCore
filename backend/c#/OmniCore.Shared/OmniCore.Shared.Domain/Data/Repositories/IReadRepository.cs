namespace OmniCore.Shared.Domain.Repositories;

using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.Pagination;
using OmniCore.Shared.Domain.Queries;
using OmniCore.Shared.Domain.Specifications;

public interface IReadRepository<TEntity, TId> 
    where TEntity : AggregateRoot<TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<DomainPagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> spec, PagedQuery query, CancellationToken cancellationToken = default);
    Task<DomainPagedResult<TEntity>> GetAdvancedPagedAsync(ISpecification<TEntity> spec, AdvancedPagedQuery query, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<TEntity>? spec = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}