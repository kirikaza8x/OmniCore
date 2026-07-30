namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;
using OmniCore.Shared.Infrastructure.EventBus;

/// <summary>
/// Extension methods for configuring MassTransit message broker integration and registering integration event handlers.
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Registers and configures MassTransit with RabbitMQ, scanning assemblies for consumers, sagas, activities, and custom integration handlers.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance for section binding.</param>
    /// <param name="assemblies">The assemblies to scan for message handlers and consumers.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMassTransitWithAssemblies(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. Bind Options properly into DI
        services.Configure<MessageBrokerConfig>(configuration.GetSection("MessageBroker"));

        // 2. Register IEventBus implementation
        services.AddScoped<IEventBus, EventBus>();

        // 3. Auto-register all IIntegrationEventHandler<T> into DI via Scrutor
        RegisterIntegrationEventHandlers(services, assemblies);

        // 4. Configure MassTransit
        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();
            busConfigurator.SetInMemorySagaRepositoryProvider();

            // Register standard MassTransit consumers
            busConfigurator.AddConsumers(assemblies);

            // Dynamically register IntegrationEventConsumer<T> wrappers
            RegisterIntegrationEventConsumers(busConfigurator, assemblies);

            busConfigurator.AddSagaStateMachines(assemblies);
            busConfigurator.AddSagas(assemblies);
            busConfigurator.AddActivities(assemblies);

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                var brokerConfig = context.GetRequiredService<IOptions<MessageBrokerConfig>>().Value;

                configurator.Host(new Uri(brokerConfig.Host), host =>
                {
                    host.Username(brokerConfig.Username);
                    host.Password(brokerConfig.Password);
                });

                configurator.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static void RegisterIntegrationEventHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes
                .AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
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