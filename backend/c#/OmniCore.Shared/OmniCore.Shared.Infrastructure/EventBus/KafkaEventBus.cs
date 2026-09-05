namespace OmniCore.Shared.Infrastructure.EventBus;

using System.Reflection;
using MassTransit;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Contracts.Events;

/// <summary>
/// Persistent log event publisher using ITopicProducerProvider (for Kafka).
/// </summary>
public sealed class KafkaEventBus(ITopicProducerProvider topicProducerProvider) : IEventBus
{
    private static readonly MethodInfo ProduceOneMethod = typeof(KafkaEventBus)
        .GetMethod(nameof(ProduceOne), BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException($"Could not locate {nameof(ProduceOne)} via reflection.");

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var runtimeType = @event.GetType();
        var task = (Task)ProduceOneMethod
            .MakeGenericMethod(runtimeType)
            .Invoke(this, new object[] { @event, cancellationToken })!;

        await task;
    }

    public async Task PublishAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events as IReadOnlyList<TEvent> ?? events.ToList();
        if (eventList.Count == 0) return;

        foreach (var @event in eventList)
        {
            await PublishAsync(@event, cancellationToken);
        }
    }

    private async Task ProduceOne<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
    {
        var topicName = ToKebabCase(typeof(TEvent).Name);
        
        // Specifying <string, TEvent> resolves ITopicProducer<string, TEvent> which natively accepts a key parameter
        var producer = topicProducerProvider.GetProducer<string, TEvent>(new Uri($"topic:{topicName}"));

        var partitionKey = @event is IPartitionedIntegrationEvent partitioned
            ? partitioned.PartitionKey
            : @event.Id.ToString();

        await producer.Produce(partitionKey, @event, cancellationToken);
    }

    private static string ToKebabCase(string str) =>
        System.Text.RegularExpressions.Regex
            .Replace(str, "(?<!^)([A-Z])", "-$1")
            .ToLowerInvariant();
}