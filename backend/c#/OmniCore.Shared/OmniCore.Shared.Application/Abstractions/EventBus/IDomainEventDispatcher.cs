namespace OmniCore.Shared.Application.Abstractions.EventBus;

using OmniCore.Shared.Domain.DDD;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}