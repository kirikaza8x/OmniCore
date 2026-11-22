namespace OmniCore.Services.User.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniCore.Services.User.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Domain.Repositories;
using OmniCore.Shared.Infrastructure;
using OmniCore.Shared.Infrastructure.Configs.Database;
using OmniCore.Shared.Infrastructure.Data;
using OmniCore.Shared.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedInfrastructure(configuration, [InfrastructureAssemblyReference.Assembly]);

        services.AddDbContext<UserDbContext>((sp, options) =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseConfig>>().Value;

            options.UseNpgsql(dbConfig.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", UserDbContext.SchemaName);

                if (dbConfig.MaxRetryCount > 0)
                    npgsqlOptions.EnableRetryOnFailure(dbConfig.MaxRetryCount);

                if (dbConfig.CommandTimeout > 0)
                    npgsqlOptions.CommandTimeout(dbConfig.CommandTimeout);
            })
            .UseSnakeCaseNamingConvention();

            if (dbConfig.EnableDetailedErrors) options.EnableDetailedErrors();
            if (dbConfig.EnableSensitiveDataLogging) options.EnableSensitiveDataLogging();

            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWorkBase<UserDbContext>>();

        // The integration-event handler that creates profiles lives in the Application assembly, so both assemblies are
        // scanned for handlers/consumers.
        services.AddMassTransitWithBroker<UserDbContext>(
            configuration,
            InfrastructureAssemblyReference.Assembly,
            Application.ApplicationAssemblyReference.Assembly);

        services.Scan(scan => scan
            .FromAssemblies(InfrastructureAssemblyReference.Assembly)
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddDataSeeders(InfrastructureAssemblyReference.Assembly);

        return services;
    }

    /// <summary>Applies EF migrations (and seeders) at startup.</summary>
    public static async Task<IApplicationBuilder> UseUserInfrastructureAsync(
        this IApplicationBuilder app,
        bool seedData = true)
    {
        await app.ApplyMigrationsAsync<UserDbContext>(seedData);
        return app;
    }
}
