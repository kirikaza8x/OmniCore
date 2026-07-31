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
//     /// </summary>
//     public static IServiceCollection AddSharedHealthChecks<TDbContext>(
//         this IServiceCollection services,
//         IConfiguration configuration) where TDbContext : DbContext
//     {
//         ArgumentNullException.ThrowIfNull(services);
//         ArgumentNullException.ThrowIfNull(configuration);

//         var healthBuilder = services.AddHealthChecks()
//             .AddDbContextCheck<TDbContext>("database_health_check", tags: new[] { "ready", "db" });

//         var redisConnectionString = configuration.GetConnectionString("Redis");
//         if (!string.IsNullOrWhiteSpace(redisConnectionString))
//         {
//             healthBuilder.AddRedis(redisConnectionString, name: "redis_health_check", tags: new[] { "ready", "cache" });
//         }

//         var rabbitMqHost = configuration.GetSection("MessageBroker")["Host"];
//         if (!string.IsNullOrWhiteSpace(rabbitMqHost))
//         {
//             healthBuilder.AddRabbitMQ(
//                 rabbitConnectionString: rabbitMqHost, 
//                 name: "rabbitmq_health_check", 
//                 tags: new[] { "ready", "messaging" });
//         }

//         return services;
//     }
// }