namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using MassTransit;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;
using OmniCore.Shared.Infrastructure.EventBus;

internal static class KafkaExtensions
{
    public static void ConfigureKafkaBus(
        this IBusRegistrationConfigurator busConfigurator,
        MessageBrokerConfig brokerConfig,
        Assembly[] assemblies)
    {
        busConfigurator.AddRider(rider =>
        {
            RegisterKafkaProducers(rider, assemblies);
            rider.AddConsumers(assemblies);
            RegisterIntegrationEventConsumersForRider(rider, assemblies);

            rider.UsingKafka((context, k) =>
            {
                k.Host(brokerConfig.KafkaBootstrapServers);
                RegisterKafkaTopicEndpoints(k, context, assemblies);
            });
        });

        busConfigurator.UsingInMemory((context, configurator) =>
        {
            configurator.ConfigureEndpoints(context);
        });
    }

    private static void RegisterKafkaProducers(IRiderRegistrationConfigurator rider, Assembly[] assemblies)
    {
        var addProducerMethod = FindAddProducerMethod();

        foreach (var eventType in assemblies.GetIntegrationEventTypes())
        {
            var topicName = eventType.Name.ToKebabCase();
            var genericMethod = addProducerMethod.MakeGenericMethod(typeof(string), eventType);

            var parameters = genericMethod.GetParameters();
            var args = new object?[parameters.Length];
            args[0] = rider;
            args[1] = topicName;

            genericMethod.Invoke(null, args);
        }
    }

    private static MethodInfo FindAddProducerMethod()
    {
        var method = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .FirstOrDefault(m => m.Name == "AddProducer"
                                 && m.IsGenericMethod
                                 && m.GetGenericArguments().Length == 2
                                 && m.GetParameters().Length >= 2
                                 && m.GetParameters()[0].ParameterType == typeof(IRiderRegistrationConfigurator));

        return method ?? throw new InvalidOperationException(
            "Could not locate MassTransit Kafka AddProducer<TKey, TValue> extension method. " +
            "Ensure the 'MassTransit.Kafka' package is referenced in your project.");
    }

    private static void RegisterKafkaTopicEndpoints(IKafkaFactoryConfigurator k, IRiderRegistrationContext context, Assembly[] assemblies)
    {
        var configureMethod = typeof(KafkaExtensions)
            .GetMethod(nameof(ConfigureTopicEndpoint), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Could not locate {nameof(ConfigureTopicEndpoint)} via reflection.");

        foreach (var eventType in assemblies.GetHandledEventTypes())
        {
            var consumerType = typeof(IntegrationEventConsumer<>).MakeGenericType(eventType);
            var topicName = eventType.Name.ToKebabCase();
            var groupId = $"{topicName}-consumer";

            var genericMethod = configureMethod.MakeGenericMethod(eventType, consumerType);
            genericMethod.Invoke(null, new object[] { k, context, groupId });
        }
    }

    private static void ConfigureTopicEndpoint<TEvent, TConsumer>(
        IKafkaFactoryConfigurator kafka,
        IRiderRegistrationContext context,
        string groupId)
        where TEvent : class
        where TConsumer : class, IConsumer<TEvent>
    {
        var topicName = typeof(TEvent).Name.ToKebabCase();
        kafka.TopicEndpoint<string, TEvent>(topicName, groupId, e =>
        {
            e.ConfigureConsumer<TConsumer>(context);
        });
    }

    private static void RegisterIntegrationEventConsumersForRider(IRiderRegistrationConfigurator rider, Assembly[] assemblies)
    {
        foreach (var eventType in assemblies.GetHandledEventTypes())
        {
            var consumerType = typeof(IntegrationEventConsumer<>).MakeGenericType(eventType);
            rider.AddConsumer(consumerType);
        }
    }
}