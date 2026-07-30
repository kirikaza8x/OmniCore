namespace OmniCore.Shared.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.Specifications;

public partial class RepositoryBase<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    /// <summary>
    /// Marks an entity as added in the DbContext change tracker.
    /// </summary>
    public virtual void Add(TEntity entity) => DbSet.Add(entity);

    /// <summary>
    /// Marks a collection of entities as added in the DbContext change tracker.
    /// </summary>
    public virtual void AddRange(IEnumerable<TEntity> entities) => DbSet.AddRange(entities);

    /// <summary>
    /// Marks an entity as modified in the DbContext change tracker.
    /// </summary>
    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    /// <summary>
    /// Marks an entity for deletion in the DbContext change tracker.
    /// </summary>
    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);

    /// <summary>
    /// Marks a collection of entities for deletion in the DbContext change tracker.
    /// </summary>
    public virtual void RemoveRange(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);

    /// <summary>
    /// Updates an existing entity or detaches tracking conflicts if an entity with the same key is already locally tracked.
    /// </summary>
    public virtual void Upsert(TEntity entity)
    {
        TEntity? trackedEntity = DbSet.Local.FirstOrDefault(e => e.Id != null && e.Id.Equals(entity.Id));

        if (trackedEntity is not null)
        {
            Context.Entry(trackedEntity).State = EntityState.Detached;
        }

        DbSet.Update(entity);
    }

    /// <summary>
    /// Asynchronously checks if an entity matching the specification exists. 
    /// Adds a new record if missing, or updates property values if existing.
    /// </summary>
    public virtual async Task UpsertAsync(
        TEntity entity, 
        ISpecification<TEntity> spec, 
        CancellationToken cancellationToken = default)
    {
        TEntity? existing = await ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            DbSet.Add(entity);
        }
        else
        {
            Context.Entry(existing).CurrentValues.SetValues(entity);
        }
    }
}