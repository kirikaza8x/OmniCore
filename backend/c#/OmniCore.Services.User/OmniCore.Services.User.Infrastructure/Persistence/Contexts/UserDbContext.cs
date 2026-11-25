namespace OmniCore.Services.User.Infrastructure.Persistence.Contexts;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Infrastructure.Data.Tenancy;

public class UserDbContext : DbContext, ITenantFilterSource
{
    public const string SchemaName = "users";

    private readonly ITenantContext? _tenantContext;

    /// <param name="tenantContext">Null at design time (dotnet ef): the tenant filter then matches nothing.</param>
    public UserDbContext(DbContextOptions<UserDbContext> options, ITenantContext? tenantContext = null) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<UserTenantProfile> TenantProfiles => Set<UserTenantProfile>();

    // Read by the query filter on every query (ADR 0001).
    TenantId ITenantFilterSource.FilterTenant => _tenantContext?.TenantId ?? default;
    bool ITenantFilterSource.FilterDisabled => _tenantContext?.IsSystem ?? false;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.UseTenantIdConversion();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // "user" is a reserved word in PostgreSQL, so the schema is called "users".
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);

        // Adds the tenant filter to every tenant-scoped entity (after the configurations, so the entities exist).
        modelBuilder.ApplyTenantQueryFilters(this);

        // MassTransit outbox and inbox tables (the consumer of Auth's event is deduplicated by the inbox).
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
