using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Events;

/// <summary>
/// Published when a new Account aggregate is successfully registered.
/// </summary>
/// <param name="AccountId">The unique ID of the created account.</param>
/// <param name="Email">The registered email address.</param>
public record AccountCreatedDomainEvent(AccountId AccountId, string Email) : DomainEvent;

/// <summary>
/// Published when an account's email verification process completes successfully.
/// </summary>
public record EmailConfirmedDomainEvent(AccountId AccountId) : DomainEvent;

/// <summary>
/// Published whenever an account changes or resets its password credential.
/// </summary>
public record PasswordChangedDomainEvent(AccountId AccountId) : DomainEvent;