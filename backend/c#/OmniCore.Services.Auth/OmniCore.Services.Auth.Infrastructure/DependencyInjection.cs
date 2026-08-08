namespace OmniCore.Services.Auth.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Domain.Repositories;
using OmniCore.Shared.Infrastructure;
using OmniCore.Shared.Infrastructure.Configs.Database;
using OmniCore.Shared.Infrastructure.Data;
using OmniCore.Shared.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Invoke General Shared Infrastructure (Auto-scans and binds DatabaseConfig to IOptions)
        services.AddSharedInfrastructure(
            configuration, 
            [InfrastructureAssemblyReference.Assembly]);

        // 2. Service-Specific DbContext Setup using validated IOptions<DatabaseConfig>
        services.AddDbContext<AuthDbContext>((sp, options) =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseConfig>>().Value;

            options.UseNpgsql(dbConfig.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", AuthDbContext.SchemaName);

                if (dbConfig.MaxRetryCount > 0)
                    npgsqlOptions.EnableRetryOnFailure(dbConfig.MaxRetryCount);

                if (dbConfig.CommandTimeout > 0)
                    npgsqlOptions.CommandTimeout(dbConfig.CommandTimeout);
            })
            .UseSnakeCaseNamingConvention();

            if (dbConfig.EnableDetailedErrors) options.EnableDetailedErrors();
            if (dbConfig.EnableSensitiveDataLogging) options.EnableSensitiveDataLogging();
        });

        // 3. Register Shared UnitOfWork tied to AuthDbContext
        services.AddScoped<IUnitOfWork, UnitOfWorkBase<AuthDbContext>>();

        // 4. Service-Specific Messaging, Outbox & Inbox Quartz Jobs
        // services.AddOutboxAndInbox<AuthDbContext>();
        services.AddMassTransitWithBroker<AuthDbContext>(
            configuration, 
            InfrastructureAssemblyReference.Assembly);

        // 5. Service-Specific Repositories & Domain Services Registration via Scrutor
        services.Scan(scan => scan
            .FromAssemblies(InfrastructureAssemblyReference.Assembly)
            
            // Register Repositories (e.g., AccountRepository -> IAccountRepository)
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            
            // Register Auth Domain/Infra Services (e.g., PasswordHasher, JwtTokenGenerator)
            .AddClasses(classes => classes.Where(type => 
                type.Name.EndsWith("Hasher") || 
                type.Name.EndsWith("Generator") ||
                type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        
        return services;
    }
}