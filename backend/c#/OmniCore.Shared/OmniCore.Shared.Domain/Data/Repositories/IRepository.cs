namespace OmniCore.Shared.Domain.Repositories;

using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.Specifications;

public interface IRepository<TEntity, TId> 
    where TEntity : AggregateRoot<TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);
    
    void Upsert(TEntity entity);
    Task UpsertAsync(TEntity entity, ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
}