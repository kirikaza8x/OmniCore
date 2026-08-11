namespace OmniCore.Services.Auth.Application.Features.Auth.EventHandlers;

using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;

public sealed class AccountCacheInvalidationEventHandler(
    ICacheService cacheService) 
    : IDomainEventHandler<PasswordChangedDomainEvent>,
      IDomainEventHandler<EmailConfirmedDomainEvent>
{
    public Task HandleAsync(PasswordChangedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => InvalidateCacheAsync(domainEvent.AccountId.Value, cancellationToken);

    public Task HandleAsync(EmailConfirmedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        => InvalidateCacheAsync(domainEvent.AccountId.Value, cancellationToken);

    private Task InvalidateCacheAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var cacheKey = $"auth:account:id:{accountId}";
        return cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}