namespace OmniCore.Services.Auth.Application.Abstractions.Security;

public interface IRefreshTokenService
{
    int RefreshTokenExpiryDays { get; }

    /// <summary>Session cap per account, or null when no cap is configured (ADR 0003, A10).</summary>
    int? MaxActiveSessions { get; }

    /// <summary>
    /// Generates a cryptographically secure random refresh token string and its validity duration.
    /// </summary>
    (string Token, TimeSpan Duration) GenerateRefreshToken();
}