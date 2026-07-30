namespace OmniCore.Shared.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Domain.Repositories;
using OmniCore.Shared.Domain.Specifications;
using OmniCore.Shared.Infrastructure.Specifications;

public partial class RepositoryBase<TEntity, TId>(DbContext context) 
    : IRepository<TEntity, TId>, IReadRepository<TEntity, TId>, IBulkRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    protected readonly DbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    protected virtual IQueryable<TEntity> ApplySpecification(ISpecification<TEntity>? spec)
    {
        return SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), spec);
    }
}