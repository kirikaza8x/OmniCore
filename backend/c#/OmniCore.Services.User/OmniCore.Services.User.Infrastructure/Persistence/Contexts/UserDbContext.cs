namespace OmniCore.Services.User.Infrastructure.Persistence.Contexts;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniCore.Services.User.Domain.Entities;

public class UserDbContext : DbContext
{
    public const string SchemaName = "users";

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // "user" is a reserved word in PostgreSQL, so the schema is called "users".
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);

        // MassTransit outbox and inbox tables (the consumer of Auth's event is deduplicated by the inbox).
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
