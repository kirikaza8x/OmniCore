namespace OmniCore.Shared.Infrastructure.Extensions;

using Microsoft.Extensions.DependencyInjection;
using OmniCore.Shared.Application.Abstractions.Storage;
using OmniCore.Shared.Infrastructure.Configs.Storage;
using OmniCore.Shared.Infrastructure.Services.Storage;

/// <summary>
/// Extension methods for registering object storage infrastructure services.
/// </summary>
public static class StorageServiceExtensions
{
    /// <summary>
    /// Registers the storage configuration options and the <see cref="IStorageService"/> implementation into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddStorageService(this IServiceCollection services)
    {
        // Bind & validate StorageConfig via the ConfigBase pipeline
        services.AddConfig<StorageConfig>();

        // Register MinIO / S3 implementation of IStorageService
        services.AddSingleton<IStorageService, MinioStorageService>();

        return services;
    }
}