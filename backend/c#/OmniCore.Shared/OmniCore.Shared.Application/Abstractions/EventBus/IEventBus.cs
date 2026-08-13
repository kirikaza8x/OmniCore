namespace OmniCore.Shared.Application.Abstractions.EventBus;

using OmniCore.Shared.Contracts.Events;

/// <summary>
/// Defines a contract for publishing integration events across the message broker infrastructure.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a single integration event to configured message transports (e.g., RabbitMQ, Kafka).
    /// </summary>
    /// <typeparam name="TEvent">The type of integration event.</typeparam>
    /// <param name="event">The integration event instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    /// <summary>
    /// Publishes a batch of integration events to configured message transports.
    /// </summary>
    /// <typeparam name="TEvent">The type of integration event.</typeparam>
    /// <param name="events">The collection of integration events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(
        IEnumerable<TEvent> events, 
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}