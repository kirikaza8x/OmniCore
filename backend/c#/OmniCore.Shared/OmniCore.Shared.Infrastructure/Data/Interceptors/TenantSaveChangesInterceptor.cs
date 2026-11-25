namespace OmniCore.Shared.Infrastructure.Data.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Domain.DDD;

/// <summary>
/// ADR 0001: stamps the active tenant on new <see cref="ITenantScoped"/> rows and refuses to write a row of another
/// tenant. A no-op for services without tenant-scoped entities and inside a system scope.
/// </summary>
public sealed class TenantSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null || tenantContext.IsSystem)
        {
            return;
        }

        foreach (EntityEntry entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            TenantId active = tenantContext.TenantId
                ?? throw new InvalidOperationException(
                    $"Cannot save {entry.Metadata.ClrType.Name}: there is no active tenant.");

            var property = entry.Property(nameof(ITenantScoped.TenantId));
            var stored = (TenantId)property.CurrentValue!;

            if (entry.State == EntityState.Added && stored.Value == Guid.Empty)
            {
                property.CurrentValue = active;
            }
            else if (stored != active)
            {
                throw new InvalidOperationException(
                    $"Cannot save {entry.Metadata.ClrType.Name}: it belongs to another tenant.");
            }
        }
    }
}
