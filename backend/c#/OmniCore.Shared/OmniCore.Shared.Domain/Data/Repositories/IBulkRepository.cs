namespace OmniCore.Shared.Domain.Repositories;

using OmniCore.Shared.Domain.DDD;

public interface IBulkRepository<TEntity, TId> 
    where TEntity : AggregateRoot<TId>
{
    Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task BulkMergeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}