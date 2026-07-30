namespace OmniCore.Shared.Infrastructure;

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Infrastructure.Configs;
using OmniCore.Shared.Infrastructure.Extensions;
using OmniCore.Shared.Infrastructure.Services.Caching;
using OmniCore.Shared.Infrastructure.Services.Time;
using Quartz;
using StackExchange.Redis;

/// <summary>
/// Assembly marker class used for scanning and referencing the Shared Infrastructure assembly.
/// </summary>
public sealed class SharedInfrastructureAssemblyReference
{
    /// <summary>
    /// Gets the <see cref="Assembly"/> instance of the shared infrastructure project.
    /// </summary>
    public static readonly Assembly Assembly = typeof(SharedInfrastructureAssemblyReference).Assembly;
}

/// <summary>
/// Root dependency injection registrar for the shared infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all core shared infrastructure services, interceptors, configurations, distributed caching, and logging pipelines using extension methods.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="moduleAssemblies">Optional additional module assemblies to scan and register configurations/interceptors from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[]? moduleAssemblies = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Combine Shared Infrastructure assembly with any passed module assemblies
        var assembliesToScan = new List<Assembly> { SharedInfrastructureAssemblyReference.Assembly };
        if (moduleAssemblies is { Length: > 0 })
        {
            assembliesToScan.AddRange(moduleAssemblies);
        }

        var allAssemblies = assembliesToScan.Distinct().ToArray();

        // 1. Automatic Options/Config Discovery across ALL provided assemblies
        services.AddOptions();
        services.RegisterAllConfigurations(allAssemblies);

        // 2. Automatic EF Core Interceptors Discovery across ALL provided assemblies via Scrutor
        services.Scan(scan => scan
            .FromAssemblies(allAssemblies)
            .AddClasses(classes => classes.AssignableTo<ISaveChangesInterceptor>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // 3. HTTP Context & Global JSON Options
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // 4. Core System Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // 5. Modular Extensions Invocation
        services.AddCurrentUserService();
        services.AddStorageService();

        // 6. Distributed Caching Engine
        ConfigureCaching(services, configuration);

        // 7. SignalR Setup
        services.AddSignalR();

        // 8. Quartz Job Scheduler
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }

    private static IServiceCollection RegisterAllConfigurations(
        this IServiceCollection services, 
        Assembly[] assemblies)
    {
        var configTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ConfigBase).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

        var addConfigMethod = typeof(OmniCoreConfigurationExtensions)
            .GetMethod(nameof(OmniCoreConfigurationExtensions.AddConfig));

        if (addConfigMethod is null) return services;

        foreach (var configType in configTypes)
        {
            var genericMethod = addConfigMethod.MakeGenericMethod(configType);
            genericMethod.Invoke(null, new object[] { services });
        }

        return services;
    }

    private static void ConfigureCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? configuration.GetSection("Redis")["ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration.GetSection("Redis")["InstanceName"] ?? "OmniCore_";
            });

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, CacheService>();
    }
}