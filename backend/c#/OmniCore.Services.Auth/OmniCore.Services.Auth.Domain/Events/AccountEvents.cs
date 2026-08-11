namespace OmniCore.Services.Auth.Domain.Events;

using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

/// <summary>
/// Published when a new Account aggregate is successfully registered.
/// </summary>
/// <param name="AccountId">The unique ID of the created account.</param>
/// <param name="Username">The registered username handle.</param>
/// <param name="Email">The registered email address (optional).</param>
public record AccountCreatedDomainEvent(
    AccountId AccountId, 
    string Username, 
    string? Email) : DomainEvent;

/// <summary>
/// Published when an account's email verification process completes successfully.
/// </summary>
public record EmailConfirmedDomainEvent(AccountId AccountId) : DomainEvent;

/// <summary>
/// Published whenever an account changes or resets its password credential.
/// </summary>
public record PasswordChangedDomainEvent(AccountId AccountId) : DomainEvent;

/// <summary>
/// Published when a role is assigned to an account.
/// </summary>
public record AccountRoleAssignedDomainEvent(
    AccountId AccountId, 
    RoleId RoleId) : DomainEvent;

/// <summary>
/// Published when a role is removed from an account.
/// </summary>
public record AccountRoleRemovedDomainEvent(
    AccountId AccountId, 
    RoleId RoleId) : DomainEvent;