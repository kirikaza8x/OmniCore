namespace OmniCore.Shared.Infrastructure.EventBus;

using MassTransit;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Contracts.Events;

public class IntegrationEventConsumer<TIntegrationEvent>(
    IEnumerable<IIntegrationEventHandler<TIntegrationEvent>> handlers,
    ILogger<IntegrationEventConsumer<TIntegrationEvent>> logger)
    : IConsumer<TIntegrationEvent> where TIntegrationEvent : class, IIntegrationEvent
{
    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent message = context.Message;

        logger.LogInformation(
            "Processing integration event {EventId} ({EventType}).",
            message.Id,
            typeof(TIntegrationEvent).Name);

        var tasks = handlers.Select(h => h.HandleAsync(message, context.CancellationToken));
        await Task.WhenAll(tasks);
    }
}