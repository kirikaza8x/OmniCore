namespace OmniCore.Services.Auth.Contracts.Events;

using OmniCore.Shared.Contracts.Events;

/// <summary>
/// A user asked to reset the password. <c>Token</c> is the raw single-use code that goes into the e-mailed link; it
/// expires after <c>ExpiresInMinutes</c>. Consumers must not log it.
/// </summary>
public record PasswordResetRequestedIntegrationEvent(
    Guid AccountId,
    string Username,
    string Email,
    string Token,
    int ExpiresInMinutes) : IntegrationEvent;
