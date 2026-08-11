namespace OmniCore.Shared.Infrastructure.EventBus;

using MassTransit;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Contracts.Events;

public sealed class EventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        
        await publishEndpoint.Publish(@event, @event.GetType(), cancellationToken);
    }

    public async Task PublishAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events as IReadOnlyList<TEvent> ?? events.ToList();
        if (eventList.Count == 0) return;

        await publishEndpoint.PublishBatch(eventList, cancellationToken);
    }
}