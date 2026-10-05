// Events/UserProfileCreatedDomainEvent.cs
namespace OmniCore.Services.User.Domain.Events;

using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

public sealed record UserProfileCreatedDomainEvent(UserId UserId, string Username, string Email) : DomainEvent;