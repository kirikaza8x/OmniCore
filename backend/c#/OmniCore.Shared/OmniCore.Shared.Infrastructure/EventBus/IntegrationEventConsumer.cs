namespace OmniCore.Shared.Infrastructure.EventBus;

using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Contracts.Events;
using OmniCore.Shared.Infrastructure.Inbox;

/// <summary>
/// Generic MassTransit consumer wrapper that enforces idempotent integration event handling via the Inbox pattern.
/// </summary>
public class IntegrationEventConsumer<TIntegrationEvent> : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    private readonly DbContext _dbContext;
    private readonly IEnumerable<IIntegrationEventHandler<TIntegrationEvent>> _handlers;
    private readonly ILogger<IntegrationEventConsumer<TIntegrationEvent>> _logger;

    public IntegrationEventConsumer(
        DbContext dbContext,
        IEnumerable<IIntegrationEventHandler<TIntegrationEvent>> handlers,
        ILogger<IntegrationEventConsumer<TIntegrationEvent>> logger)
    {
        _dbContext = dbContext;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent message = context.Message;

        // 1. Check if message was already processed in Inbox
        bool alreadyProcessed = await _dbContext.Set<InboxMessage>()
            .AnyAsync(i => i.Id == message.Id && i.ProcessedOnUtc != null, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Inbox event {EventId} ({EventType}) already processed. Skipping.", 
                message.Id, typeof(TIntegrationEvent).Name);
            return;
        }

        // 2. Record incoming message in Inbox
        var inboxMessage = new InboxMessage
        {
            Id = message.Id,
            Type = typeof(TIntegrationEvent).AssemblyQualifiedName ?? typeof(TIntegrationEvent).FullName!,
            Content = JsonSerializer.Serialize(message),
            OccurredOnUtc = message.OccurredOnUtc
        };

        _dbContext.Set<InboxMessage>().Add(inboxMessage);

        try
        {
            // 3. Sequentially execute all registered application handlers
            foreach (var handler in _handlers)
            {
                await handler.HandleAsync(message, context.CancellationToken);
            }

            // 4. Mark inbox message as processed
            inboxMessage.ProcessedOnUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbox event {EventId} ({EventType}). Saved error for retry job.", 
                message.Id, typeof(TIntegrationEvent).Name);

            inboxMessage.Error = ex.Message;
            inboxMessage.RetryCount++;

            await _dbContext.SaveChangesAsync(context.CancellationToken);
            throw; // Re-throw so MassTransit retry policy handles it
        }
    }
}