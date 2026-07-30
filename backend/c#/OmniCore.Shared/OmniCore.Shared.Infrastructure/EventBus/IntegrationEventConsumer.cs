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
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(context.Message, context.CancellationToken);
        }
    }
}