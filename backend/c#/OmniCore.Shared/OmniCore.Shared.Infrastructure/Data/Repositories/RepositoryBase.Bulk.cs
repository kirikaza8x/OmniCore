namespace OmniCore.Shared.Infrastructure.Data.Repositories;

using EFCore.BulkExtensions;
using OmniCore.Shared.Domain.DDD;

public partial class RepositoryBase<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    /// <summary>
    /// Performs high-performance bulk insertion directly into the database.
    /// <para>Note: Bypasses EF Core Change Tracker and Interceptors.</para>
    /// </summary>
    public virtual async Task BulkInsertAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        var entityList = entities as List<TEntity> ?? entities.ToList();
        await Context.BulkInsertAsync(entityList, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs high-performance bulk updates directly into the database.
    /// <para>Note: Bypasses EF Core Change Tracker and Interceptors.</para>
    /// </summary>
    public virtual async Task BulkUpdateAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        var entityList = entities as List<TEntity> ?? entities.ToList();
        await Context.BulkUpdateAsync(entityList, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs high-performance bulk deletions directly in the database.
    /// <para>Note: Bypasses EF Core Change Tracker and Interceptors.</para>
    /// </summary>
    public virtual async Task BulkDeleteAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        var entityList = entities as List<TEntity> ?? entities.ToList();
        await Context.BulkDeleteAsync(entityList, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs high-performance upserts (Insert or Update) directly into the database.
    /// <para>Note: Bypasses EF Core Change Tracker and Interceptors.</para>
    /// </summary>
    public virtual async Task BulkMergeAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        var entityList = entities as List<TEntity> ?? entities.ToList();
        await Context.BulkInsertOrUpdateAsync(entityList, cancellationToken: cancellationToken);
    }
}