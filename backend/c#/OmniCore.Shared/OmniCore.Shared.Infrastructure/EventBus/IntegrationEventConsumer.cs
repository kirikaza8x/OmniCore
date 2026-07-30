namespace OmniCore.Shared.Infrastructure.EventBus;

using MassTransit;
using OmniCore.Shared.Application.Abstractions.EventBus;

public class IntegrationEventConsumer<TIntegrationEvent>(
    IEnumerable<IIntegrationEventHandler<TIntegrationEvent>> handlers) 
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        // Executes registered application handlers sequentially.
        // NOTE: Handlers must be idempotent because if a subsequent handler throws,
        // MassTransit's retry pipeline will re-execute all previous handlers in this loop.
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(context.Message, context.CancellationToken);
        }
    }
}