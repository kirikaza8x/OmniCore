namespace OmniCore.Shared.Infrastructure.Data.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Application.Abstractions.Authentication;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditableEntityInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditProperties(DbContext? context)
    {
        if (context is null) return;

        string currentUserId = _currentUser.UserId.ToString() ?? "System";
        DateTime utcNow = DateTime.UtcNow;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            // 1. Audit Trail Updates (IAuditableEntity)
            if (entry.Entity is IAuditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = currentUserId;
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = utcNow;
                }

                if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
                {
                    entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = currentUserId;
                    entry.Property(nameof(IAuditableEntity.ModifiedAt)).CurrentValue = utcNow;
                }
            }

            // 2. Soft Delete Default Initialization (ISoftDeletable)
            if (entry.Entity is ISoftDeletable && entry.State == EntityState.Added)
            {
                entry.Property(nameof(ISoftDeletable.IsActive)).CurrentValue = true;
            }
        }
    }
}

public static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added ||
             r.TargetEntry.State == EntityState.Modified ||
             r.TargetEntry.HasChangedOwnedEntities()));
}