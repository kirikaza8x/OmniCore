namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Infrastructure.Data.Seeds;

/// <summary>
/// Extension methods for database migrations and initial seeding execution.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Scans the specified assemblies using Scrutor to register all <see cref="IDataSeeder"/> implementations into DI with scoped lifetime.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDataSeeders(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo<IDataSeeder>(), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    /// <summary>
    /// Asynchronously applies pending EF Core migrations for the specified DbContext and executes registered data seeders.
    /// </summary>
    /// <typeparam name="TContext">The type of <see cref="DbContext"/> to migrate.</typeparam>
    /// <param name="app">The application builder.</param>
    /// <param name="seedData">When <c>true</c>, runs registered <see cref="IDataSeeder"/> instances after migration completes.</param>
    /// <returns>The application builder instance for chaining.</returns>
    public static async Task<IApplicationBuilder> ApplyMigrationsAsync<TContext>(
        this IApplicationBuilder app, 
        bool seedData = false) 
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.ApplicationServices.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var logger = serviceProvider.GetService<ILogger<TContext>>();

        var context = serviceProvider.GetRequiredService<TContext>();
        
        logger?.LogInformation("Applying database migrations for {DbContext}...", typeof(TContext).Name);
        await context.Database.MigrateAsync();
        logger?.LogInformation("Database migrations applied successfully for {DbContext}.", typeof(TContext).Name);

        if (seedData)
        {
            await SeedDataAsync(serviceProvider, logger);
        }

        return app;
    }

    private static async Task SeedDataAsync(IServiceProvider serviceProvider, ILogger? logger)
    {
        var seeders = serviceProvider.GetServices<IDataSeeder>()
            .OrderBy(s => s.Order)
            .ToList();

        if (seeders.Count == 0)
        {
            logger?.LogWarning("Seed data was requested, but no IDataSeeder implementations were registered in DI.");
            return;
        }

        logger?.LogInformation("Starting database seeding ({Count} seeder(s) found)...", seeders.Count);

        foreach (var seeder in seeders)
        {
            logger?.LogInformation("Executing seeder: {SeederName} (Order: {Order})", seeder.GetType().Name, seeder.Order);
            await seeder.SeedAllAsync();
        }

        logger?.LogInformation("Database seeding completed successfully.");
    }
}