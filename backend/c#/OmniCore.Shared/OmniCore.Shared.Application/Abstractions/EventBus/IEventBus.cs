using OmniCore.Shared.Contracts.Events;

namespace OmniCore.Shared.Application.Abstractions.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    Task PublishAsync<TEvent>(
        IEnumerable<TEvent> events, 
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}