namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;
using OmniCore.Shared.Infrastructure.EventBus;

/// <summary>
/// Extension methods for setting up MassTransit with dynamic broker transport and generic outbox persistence.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Configures MassTransit supporting RabbitMQ, Kafka, and configurable EF Core Outbox persistence.
    /// </summary>
    /// <typeparam name="TDbContext">The EF Core <see cref="DbContext"/> used for persistence.</typeparam>
    public static IServiceCollection AddMassTransitWithBroker<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddConfig<MessageBrokerConfig>();
        services.AddScoped<IEventBus, EventBus>();

        RegisterIntegrationEventHandlers(services, assemblies);

        var brokerConfig = configuration.GetSection("MessageBroker").Get<MessageBrokerConfig>() 
                           ?? new MessageBrokerConfig();

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            // 1. Generic Transactional Outbox Configuration
            if (brokerConfig.EnableOutbox)
            {
                busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outbox =>
                {
                    outbox.UseBusOutbox();

                    switch (brokerConfig.OutboxDbProvider.ToLowerInvariant())
                    {
                        case "postgres":
                        case "postgresql":
                            outbox.UsePostgres();
                            break;
                        case "sqlserver":
                        case "mssql":
                            outbox.UseSqlServer();
                            break;
                        default:
                            // Fallback to in-memory/custom outbox handling
                            break;
                    }
                });
            }

            // 2. Register MassTransit Consumers & Integration Event Adapters
            busConfigurator.AddConsumers(assemblies);
            RegisterIntegrationEventConsumers(busConfigurator, assemblies);

            // 3. Configure Message Broker Transport (RabbitMQ / Kafka)
            bool isKafka = brokerConfig.Provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase);
            bool isBoth = brokerConfig.Provider.Equals("Both", StringComparison.OrdinalIgnoreCase);

            if (isKafka || isBoth)
            {
                busConfigurator.AddRider(rider =>
                {
                    rider.AddConsumers(assemblies);
                    rider.UsingKafka((context, k) =>
                    {
                        k.Host(brokerConfig.KafkaBootstrapServers);
                    });
                });
            }

            if (!isKafka)
            {
                busConfigurator.UsingRabbitMq((context, configurator) =>
                {
                    configurator.Host(new Uri(brokerConfig.Host), host =>
                    {
                        host.Username(brokerConfig.Username);
                        host.Password(brokerConfig.Password);
                    });

                    configurator.ConfigureEndpoints(context);
                });
            }
            else
            {
                busConfigurator.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
            }
        });

        return services;
    }

    private static void RegisterIntegrationEventHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    private static void RegisterIntegrationEventConsumers(IRegistrationConfigurator config, Assembly[] assemblies)
    {
        var eventTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
            .Select(i => i.GetGenericArguments()[0])
            .Distinct()
            .ToList();

        foreach (var eventType in eventTypes)
        {
            var consumerType = typeof(IntegrationEventConsumer<>).MakeGenericType(eventType);
            config.AddConsumer(consumerType);
        }
    }
}