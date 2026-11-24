namespace OmniCore.Shared.Infrastructure.Data.Tenancy;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OmniCore.Shared.Domain.DDD;

/// <summary>Stores <see cref="TenantId"/> as a plain uuid column (ADR 0001).</summary>
public sealed class TenantIdConverter()
    : ValueConverter<TenantId, Guid>(id => id.Value, value => new TenantId(value));

/// <summary>A DbContext that applies the tenant filters implements this: the filter reads these two members per query.</summary>
public interface ITenantFilterSource
{
    /// <summary>The tenant whose rows are visible (default value matches nothing).</summary>
    TenantId FilterTenant { get; }

    /// <summary>True inside a system scope: no filtering.</summary>
    bool FilterDisabled { get; }
}

public static class TenantEfExtensions
{
    /// <summary>In ConfigureConventions: every <see cref="TenantId"/> property is a uuid column.</summary>
    public static ModelConfigurationBuilder UseTenantIdConversion(this ModelConfigurationBuilder builder)
    {
        builder.Properties<TenantId>().HaveConversion<TenantIdConverter>();
        return builder;
    }

    /// <summary>
    /// In OnModelCreating, after the entity configurations: adds a query filter on every entity that implements
    /// <see cref="ITenantScoped"/>, by convention, so a service cannot forget it. Pass <c>this</c>.
    /// </summary>
    public static ModelBuilder ApplyTenantQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantFilterSource
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => t.BaseType is null && typeof(ITenantScoped).IsAssignableFrom(t.ClrType))
                     .ToList())
        {
            ParameterExpression entity = Expression.Parameter(entityType.ClrType, "e");
            Expression ctx = Expression.Constant(context, typeof(TContext));

            Expression disabled = Expression.Property(ctx, nameof(ITenantFilterSource.FilterDisabled));
            Expression current = Expression.Property(ctx, nameof(ITenantFilterSource.FilterTenant));
            Expression stored = Expression.Call(
                typeof(EF), nameof(EF.Property), [typeof(TenantId)],
                entity, Expression.Constant(nameof(ITenantScoped.TenantId)));

            Expression body = Expression.OrElse(disabled, Expression.Equal(stored, current));
            LambdaExpression filter = Expression.Lambda(body, entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        return modelBuilder;
    }
}
