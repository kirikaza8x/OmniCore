namespace OmniCore.Shared.Application.Abstractions.Outbox;

using OmniCore.Shared.Application.Abstractions.EventBus;

public interface IOutboxService
{
    Task EnqueueAsync<T>(
        T integrationEvent, 
        CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent;
}