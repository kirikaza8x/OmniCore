namespace OmniCore.Shared.Application.Abstractions.Messaging;

using OmniCore.Shared.Domain.DDD;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}