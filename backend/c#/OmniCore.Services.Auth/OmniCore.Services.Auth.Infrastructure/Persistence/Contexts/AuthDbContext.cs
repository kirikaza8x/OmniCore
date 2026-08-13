using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;

public class AuthDbContext : DbContext
{
    public const string SchemaName = "auth";
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<MfaMethod> MfaMethods => Set<MfaMethod>();
    public DbSet<MfaRecoveryCode> MfaRecoveryCodes => Set<MfaRecoveryCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
    public DbSet<SecurityToken> SecurityTokens => Set<SecurityToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Sets default PostgreSQL schema to "auth"
        modelBuilder.HasDefaultSchema(SchemaName);

        // Applies all IEntityTypeConfiguration classes in the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        modelBuilder.AddTransactionalOutboxEntities();
        // modelBuilder.Entity<AccountRole>().HasQueryFilter(x => !x.Account.IsDeleted);
        // modelBuilder.Entity<ExternalLogin>().HasQueryFilter(x => !x.Account.IsDeleted);
        // modelBuilder.Entity<MfaMethod>().HasQueryFilter(x => !x.Account.IsDeleted);
        // modelBuilder.Entity<MfaRecoveryCode>().HasQueryFilter(x => !x.Account.IsDeleted);
        // modelBuilder.Entity<RefreshToken>().HasQueryFilter(x => !x.Account.IsDeleted);
        // modelBuilder.Entity<SecurityToken>().HasQueryFilter(x => !x.Account.IsDeleted);
    }
}