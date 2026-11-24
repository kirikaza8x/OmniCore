namespace OmniCore.Services.Auth.Contracts.Events;

using OmniCore.Shared.Contracts.Events;

/// <summary>
/// A user asked for an e-mail verification link. <c>Token</c> is the raw single-use code; consumers must not log it.
/// </summary>
public record EmailVerificationRequestedIntegrationEvent(
    Guid AccountId,
    string Username,
    string Email,
    string Token,
    int ExpiresInMinutes) : IntegrationEvent;
