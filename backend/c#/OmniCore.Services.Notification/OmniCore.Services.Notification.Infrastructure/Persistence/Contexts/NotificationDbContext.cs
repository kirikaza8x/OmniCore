namespace OmniCore.Services.Notification.Infrastructure.Persistence.Contexts;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Notification.Domain.Entities;

public class NotificationDbContext : DbContext
{
    public const string SchemaName = "notification";

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);

        // MassTransit outbox and inbox tables (consumers are deduplicated by the inbox).
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
