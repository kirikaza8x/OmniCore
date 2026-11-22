namespace OmniCore.Services.User.Domain.Events;

using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

public sealed record UserProfileDeletedDomainEvent(UserId UserId) : DomainEvent;
