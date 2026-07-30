namespace OmniCore.Shared.Application.Abstractions.EventBus;

public abstract class IntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    public abstract Task HandleAsync(
        TIntegrationEvent integrationEvent, 
        CancellationToken cancellationToken = default);
}