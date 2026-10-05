// Features/UserProfiles/EventHandlers/UserProfileCacheInvalidationEventHandler.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.EventHandlers;

using OmniCore.Services.User.Application.Features.UserProfiles.Queries.GetUserProfileById;
using OmniCore.Services.User.Domain.Events;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Helpers;

public sealed class UserProfileCacheInvalidationEventHandler(
    ICacheService cacheService) 
    : IDomainEventHandler<UserProfileCreatedDomainEvent>,
      IDomainEventHandler<UserProfileUpdatedDomainEvent>,
      IDomainEventHandler<UserAvatarUpdatedDomainEvent>
{
    public Task HandleAsync(
        UserProfileCreatedDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        return InvalidateCacheAsync(domainEvent.UserId.Value, cancellationToken);
    }

    public Task HandleAsync(
        UserProfileUpdatedDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        return InvalidateCacheAsync(domainEvent.UserId.Value, cancellationToken);
    }

    public Task HandleAsync(
        UserAvatarUpdatedDomainEvent domainEvent, 
        CancellationToken cancellationToken = default)
    {
        return InvalidateCacheAsync(domainEvent.UserId.Value, cancellationToken);
    }

    private Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyGeneratorHelper.For<GetUserProfileByIdQuery>(userId);
        return cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}