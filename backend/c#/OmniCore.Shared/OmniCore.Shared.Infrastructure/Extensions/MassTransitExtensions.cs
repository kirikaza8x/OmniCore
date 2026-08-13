namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Contracts.Events;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;
using OmniCore.Shared.Infrastructure.EventBus;

/// <summary>
/// Extension methods for setting up MassTransit with dynamic broker transport and generic outbox persistence.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Configures MassTransit supporting RabbitMQ, Kafka, and configurable EF Core Outbox persistence.
    /// Uses default outbox provider configured in MessageBroker appsettings.
    /// </summary>
    public static IServiceCollection AddMassTransitWithBroker<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies) where TDbContext : DbContext
    {
        return services.AddMassTransitWithBroker<TDbContext>(configuration, configureOutbox: null, assemblies);
    }

    /// <summary>
    /// Configures MassTransit supporting RabbitMQ, Kafka, and configurable EF Core Outbox persistence.
    /// Allows custom builder delegates for Outbox configuration overrides.
    /// </summary>
    public static IServiceCollection AddMassTransitWithBroker<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IEntityFrameworkOutboxConfigurator>? configureOutbox,
        params Assembly[] assemblies) where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // =========================================================================
        // Framework Configuration & Event Bus Wrappers
        // =========================================================================
        services.AddConfig<MessageBrokerConfig>();
        services.AddScoped<IEventBus, EventBus>();

        // Register domain event handlers (IIntegrationEventHandler<T>) via Scrutor
        RegisterIntegrationEventHandlers(services, assemblies);

        var brokerConfig = configuration.GetSection("MessageBroker").Get<MessageBrokerConfig>() 
                            ?? new MessageBrokerConfig();

        // =========================================================================
        // Core MassTransit Service Registration
        // =========================================================================

        // services.AddOutboxAndInbox<TDbContext>();
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            // ---------------------------------------------------------------------
            // 1. Transactional Outbox Setup (Flexible Builder / Factory Pattern)
            // ---------------------------------------------------------------------
            if (brokerConfig.EnableOutbox)
            {
                busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outbox =>
                {
                    outbox.UseBusOutbox();

                    if (configureOutbox is not null)
                    {
                        configureOutbox(outbox);
                    }
                    else
                    {
                        ApplyOutboxDbProvider(outbox, brokerConfig.OutboxDbProvider);
                    }
                });
            }

            // ---------------------------------------------------------------------
            // 2. Consumer Registrations
            // ---------------------------------------------------------------------
            busConfigurator.AddConsumers(assemblies);
            RegisterIntegrationEventConsumers(busConfigurator, assemblies);

            // ---------------------------------------------------------------------
            // 3. Message Broker Transport Configuration
            // ---------------------------------------------------------------------
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
                busConfigurator.UsingInMemory((context, configurator) => configurator.ConfigureEndpoints(context));
            }
        });

        return services;
    }

    /// <summary>
    /// Factory method to dynamically apply Outbox database provider extensions.
    /// </summary>
    private static void ApplyOutboxDbProvider(
        IEntityFrameworkOutboxConfigurator outbox,
        string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return;
        }

        switch (providerName.Trim().ToLowerInvariant())
        {
            case "postgres":
            case "postgresql":
            case "npgsql":
                outbox.UsePostgres();
                break;

            case "sqlserver":
            case "mssql":
                outbox.UseSqlServer();
                break;

            case "mysql":
            case "mariadb":
                outbox.UseMySql();
                break;

            case "sqlite":
                outbox.UseSqlite();
                break;

            default:
                throw new NotSupportedException($"Unsupported Outbox database provider: '{providerName}'.");
        }
    }

    /// <summary>
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