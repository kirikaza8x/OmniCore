namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using MassTransit;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;
using OmniCore.Shared.Infrastructure.EventBus;

internal static class RabbitMqExtensions
{
    public static void ConfigureRabbitMqBus(
        this IBusRegistrationConfigurator busConfigurator,
        MessageBrokerConfig brokerConfig,
        Assembly[] assemblies)
    {
        busConfigurator.AddConsumers(assemblies);
        RegisterIntegrationEventConsumers(busConfigurator, assemblies);

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

            configurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
            configurator.PrefetchCount = 16;
            configurator.ConfigureEndpoints(context);
        });
    }

    private static void RegisterIntegrationEventConsumers(IBusRegistrationConfigurator config, Assembly[] assemblies)
    {
        foreach (var eventType in assemblies.GetHandledEventTypes())
        {
            var consumerType = typeof(IntegrationEventConsumer<>).MakeGenericType(eventType);
            config.AddConsumer(consumerType);
        }
    }
}