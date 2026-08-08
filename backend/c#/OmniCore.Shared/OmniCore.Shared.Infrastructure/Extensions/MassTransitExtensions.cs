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

        // =========================================================================
        // [CUSTOM IMPLEMENTATION] Framework Configuration & Event Bus Wrappers
        // =========================================================================
        services.AddConfig<MessageBrokerConfig>();
        services.AddScoped<IEventBus, EventBus>();

        // [CUSTOM IMPLEMENTATION] Register domain event handlers (IIntegrationEventHandler<T>) via Scrutor
        RegisterIntegrationEventHandlers(services, assemblies);

        var brokerConfig = configuration.GetSection("MessageBroker").Get<MessageBrokerConfig>() 
                            ?? new MessageBrokerConfig();

        // =========================================================================
        // [MASSTRANSIT BUILT-IN] Core Service Registration
        // =========================================================================
        services.AddMassTransit(busConfigurator =>
        {
            // [MASSTRANSIT BUILT-IN] Formats queue & exchange names using kebab-case
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            // ---------------------------------------------------------------------
            // 1. Transactional Outbox Setup (DISABLED BUILT-IN -> USING CUSTOM QUARTZ)
            // ---------------------------------------------------------------------
            // [CUSTOM IMPLEMENTATION]
            // MassTransit's built-in EF Core outbox is disabled here.
            // Outbox processing is handled by your custom Quartz background jobs
            // (`ProcessOutboxJob`) reading from custom `OutboxMessage` tables.
            // When calling IPublishEndpoint directly inside `ProcessOutboxJob`,
            // MassTransit sends the payload straight to the underlying transport (RabbitMQ/Kafka).

            /*
            // [MASSTRANSIT BUILT-IN] Outbox Configuration (Disabled for now)
            if (brokerConfig.EnableOutbox)
            {
                busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outbox =>
                {
                    outbox.UseBusOutbox();

                    switch (brokerConfig.OutboxDbProvider?.ToLowerInvariant())
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
                            break;
                    }
                });
            }
            */

            // ---------------------------------------------------------------------
            // 2. Consumer Registrations
            // ---------------------------------------------------------------------
            // [MASSTRANSIT BUILT-IN] Register standard MassTransit IConsumer<T> classes
            busConfigurator.AddConsumers(assemblies);

            // [CUSTOM IMPLEMENTATION] Adapter registering IntegrationEventConsumer<T> wrappers for IIntegrationEventHandler<T>
            RegisterIntegrationEventConsumers(busConfigurator, assemblies);

            // ---------------------------------------------------------------------
            // 3. [MASSTRANSIT BUILT-IN] Message Broker Transport Configuration
            // ---------------------------------------------------------------------
            bool isKafka = brokerConfig.Provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase);
            bool isBoth = brokerConfig.Provider.Equals("Both", StringComparison.OrdinalIgnoreCase);

            if (isKafka || isBoth)
            {
                // [MASSTRANSIT BUILT-IN] Kafka Rider integration
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
                // [MASSTRANSIT BUILT-IN] RabbitMQ Transport Setup
                busConfigurator.UsingRabbitMq((context, configurator) =>
                {
                    if (Uri.TryCreate(brokerConfig.Host, UriKind.Absolute, out var parsedUri))
                    {
                        configurator.Host(parsedUri, host =>
                        {
                            host.Username(brokerConfig.Username);
                            host.Password(brokerConfig.Password);
                        });
                    }
                    else
                    {
                        configurator.Host(brokerConfig.Host, "/", host =>
                        {
                            host.Username(brokerConfig.Username);
                            host.Password(brokerConfig.Password);
                        });
                    }

                    configurator.ConfigureEndpoints(context);
                });
            }
            else
            {
                // [MASSTRANSIT BUILT-IN] In-Memory Transport (fallback for pure Kafka setups)
                busConfigurator.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
            }
        });

        return services;
    }

    /// <summary>
    /// [CUSTOM IMPLEMENTATION]
    /// Scans assemblies and registers your custom <see cref="IIntegrationEventHandler{T}"/> types into .NET Core DI.
    /// </summary>
    private static void RegisterIntegrationEventHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    /// <summary>
    /// [CUSTOM IMPLEMENTATION]
    /// Dynamically binds MassTransit consumers to your custom <see cref="IIntegrationEventHandler{T}"/> pattern.
    /// </summary>
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