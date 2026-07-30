namespace Microsoft.Extensions.DependencyInjection;

using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Infrastructure.Configs.Cache;
using OmniCore.Shared.Infrastructure.Services.Caching;

/// <summary>
/// Extension methods for configuring caching options and registering cache abstraction services.
/// </summary>
public static class CachingServiceExtensions
{
    /// <summary>
    /// Configures memory cache settings and registers <see cref="ICacheService"/> as a thread-safe singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Delegate to configure <see cref="MemoryCacheConfig"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCacheServices(
        this IServiceCollection services, 
        Action<MemoryCacheConfig> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }
}