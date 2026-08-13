namespace OmniCore.Shared.Infrastructure.EventBus;

using MassTransit;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Contracts.Events;

/// <summary>
/// Generic MassTransit consumer wrapper that routes integration events to registered application handlers.
/// Message deduplication and idempotency are handled natively by MassTransit.
/// </summary>
public class IntegrationEventConsumer<TIntegrationEvent> : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    private readonly IEnumerable<IIntegrationEventHandler<TIntegrationEvent>> _handlers;
    private readonly ILogger<IntegrationEventConsumer<TIntegrationEvent>> _logger;

    public IntegrationEventConsumer(
        IEnumerable<IIntegrationEventHandler<TIntegrationEvent>> handlers,
        ILogger<IntegrationEventConsumer<TIntegrationEvent>> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent message = context.Message;

        _logger.LogInformation(
            "Processing integration event {EventId} ({EventType}).", 
            message.Id, 
            typeof(TIntegrationEvent).Name);

        // Sequentially execute all registered application handlers
        foreach (var handler in _handlers)
        {
            await handler.HandleAsync(message, context.CancellationToken);
        }
    }
}