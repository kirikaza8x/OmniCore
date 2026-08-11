namespace OmniCore.Shared.Application.Abstractions.Messaging;

using MediatR;
using OmniCore.Shared.Domain.DDD;

public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);

    Task INotificationHandler<TDomainEvent>.Handle(
        TDomainEvent notification, 
        CancellationToken cancellationToken) 
        => HandleAsync(notification, cancellationToken);
}