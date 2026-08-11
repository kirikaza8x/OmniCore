namespace OmniCore.Shared.Infrastructure.EventBus;

using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Domain.DDD;

public sealed class DomainEventDispatcher(
    IPublisher publisher,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            logger.LogInformation("Dispatching Domain Event: {EventType} ({EventId})", domainEvent.EventType, domainEvent.EventId);
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}