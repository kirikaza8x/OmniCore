namespace OmniCore.Shared.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.Specifications;

public partial class RepositoryBase<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    public virtual void Add(TEntity entity) => DbSet.Add(entity);

    public virtual void AddRange(IEnumerable<TEntity> entities) => DbSet.AddRange(entities);

    public virtual void Update(TEntity entity) => DbSet.Update(entity);

    public virtual void Remove(TEntity entity) => DbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);

    public virtual void Upsert(TEntity entity)
    {
        TEntity? trackedEntity = DbSet.Local.FirstOrDefault(e => e.Id != null && e.Id.Equals(entity.Id));

        if (trackedEntity is not null)
        {
            Context.Entry(trackedEntity).State = EntityState.Detached;
        }

        DbSet.Update(entity);
    }

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