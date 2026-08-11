namespace OmniCore.Shared.Application.Abstractions.Outbox;

using OmniCore.Shared.Contracts.Events;

public interface IOutboxService
{
    Task EnqueueAsync<T>(
        T integrationEvent, 
        CancellationToken cancellationToken = default) 
        where T : class, IIntegrationEvent;
}