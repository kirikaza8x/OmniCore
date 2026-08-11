namespace OmniCore.Services.Auth.Application.Features.Roles.EventHandlers;

using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;

public sealed class AccountRoleCacheInvalidationEventHandler(
    ICacheService cacheService) 
    : IDomainEventHandler<AccountRoleAssignedDomainEvent>,
      IDomainEventHandler<AccountRoleRemovedDomainEvent>
{
    public Task HandleAsync(
        AccountRoleAssignedDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        return InvalidateCacheAsync(domainEvent.AccountId.Value, cancellationToken);
    }

    public Task HandleAsync(
        AccountRoleRemovedDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        return InvalidateCacheAsync(domainEvent.AccountId.Value, cancellationToken);
    }

    private Task InvalidateCacheAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var cacheKey = $"auth:account:id:{accountId}";
        return cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}