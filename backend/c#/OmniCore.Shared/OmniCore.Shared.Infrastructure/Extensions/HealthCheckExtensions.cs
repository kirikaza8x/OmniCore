// namespace Microsoft.Extensions.DependencyInjection;

// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Diagnostics.HealthChecks;

// /// <summary>
// /// Extension methods for configuring infrastructure health checks.
// /// </summary>
// public static class HealthCheckExtensions
// {
//     /// <summary>
//     /// Registers standard infrastructure health checks for database, redis, rabbitmq, and storage.
//     /// Each check is opt-in based on whether its configuration section/connection string is present,
//     /// so services that don't use a given dependency don't need to stub it out.
//     /// </summary>
//     public static IServiceCollection AddSharedHealthChecks<TDbContext>(
//         this IServiceCollection services,
//         IConfiguration configuration,
//         Action<SharedHealthCheckOptions>? configureOptions = null)
//         where TDbContext : DbContext
//     {
//         ArgumentNullException.ThrowIfNull(services);
//         ArgumentNullException.ThrowIfNull(configuration);

//         var options = new SharedHealthCheckOptions();
//         configureOptions?.Invoke(options);

//         var healthBuilder = services.AddHealthChecks()
//             .AddDbContextCheck<TDbContext>(
//                 options.DatabaseCheckName,
//                 tags: options.DatabaseTags);

//         var redisConnectionString = configuration.GetConnectionString(options.RedisConnectionStringKey);
//         if (!string.IsNullOrWhiteSpace(redisConnectionString))
//         {
//             healthBuilder.AddRedis(
//                 redisConnectionString,
//                 name: options.RedisCheckName,
//                 tags: options.RedisTags);
//         }

//         var rabbitMqHost = configuration.GetSection(options.MessageBrokerSectionName)["Host"];
//         if (!string.IsNullOrWhiteSpace(rabbitMqHost))
//         {
//             healthBuilder.AddRabbitMQ(
//                 rabbitConnectionString: rabbitMqHost,
//                 name: options.RabbitMqCheckName,
//                 tags: options.RabbitMqTags);
//         }

//         var storageConnectionString = configuration.GetConnectionString(options.StorageConnectionStringKey);
//         if (!string.IsNullOrWhiteSpace(storageConnectionString) && options.StorageCheckFactory is not null)
//         {
//             options.StorageCheckFactory(healthBuilder, storageConnectionString, options);
//         }

//         options.ExtraChecks?.Invoke(healthBuilder, configuration);

//         return services;
//     }
// }

// /// <summary>
// /// Configuration knobs for <see cref="HealthCheckExtensions.AddSharedHealthChecks{TDbContext}"/>.
// /// Lets each consuming service override names/tags/config keys instead of forking the extension.
// /// </summary>
// public sealed class SharedHealthCheckOptions
// {
//     public string DatabaseCheckName { get; set; } = "database_health_check";
//     public string[] DatabaseTags { get; set; } = ["ready", "db"];

//     public string RedisConnectionStringKey { get; set; } = "Redis";
//     public string RedisCheckName { get; set; } = "redis_health_check";
//     public string[] RedisTags { get; set; } = ["ready", "cache"];

//     public string MessageBrokerSectionName { get; set; } = "MessageBroker";
//     public string RabbitMqCheckName { get; set; } = "rabbitmq_health_check";
//     public string[] RabbitMqTags { get; set; } = ["ready", "messaging"];

//     public string StorageConnectionStringKey { get; set; } = "Storage";
//     public string StorageCheckName { get; set; } = "storage_health_check";
//     public string[] StorageTags { get; set; } = ["ready", "storage"];

//     /// <summary>
//     /// Plugs in whichever storage health check package the service actually references
//     /// (Azure Blob, S3, MinIO...), since the shared kernel shouldn't force a dependency on one.
//     /// </summary>
//     public Action<IHealthChecksBuilder, string, SharedHealthCheckOptions>? StorageCheckFactory { get; set; }

//     /// <summary>
//     /// Escape hatch for service-specific checks (e.g. an external API) without touching this file.
//     /// </summary>
//     public Action<IHealthChecksBuilder, IConfiguration>? ExtraChecks { get; set; }
// }