namespace OmniCore.Shared.Infrastructure.EventBus;

using MassTransit;
using OmniCore.Shared.Application.Abstractions.EventBus;

public sealed class EventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        await publishEndpoint.Publish(@event, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        await publishEndpoint.PublishBatch(events, cancellationToken);
    }
}