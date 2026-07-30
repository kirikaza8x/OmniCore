namespace OmniCore.Shared.Infrastructure.Data.Repositories;

using EFCore.BulkExtensions;
using OmniCore.Shared.Domain.DDD;

public partial class RepositoryBase<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    public virtual async Task BulkInsertAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        await Context.BulkInsertAsync(entities.ToList(), cancellationToken: cancellationToken);
    }

    public virtual async Task BulkUpdateAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        await Context.BulkUpdateAsync(entities.ToList(), cancellationToken: cancellationToken);
    }

    public virtual async Task BulkDeleteAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        await Context.BulkDeleteAsync(entities.ToList(), cancellationToken: cancellationToken);
    }

    public virtual async Task BulkMergeAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        await Context.BulkInsertOrUpdateAsync(entities.ToList(), cancellationToken: cancellationToken);
    }
}