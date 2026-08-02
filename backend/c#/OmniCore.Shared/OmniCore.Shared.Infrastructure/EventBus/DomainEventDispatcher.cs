namespace OmniCore.Shared.Infrastructure.EventBus;

using MediatR;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Domain.DDD;

public sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}