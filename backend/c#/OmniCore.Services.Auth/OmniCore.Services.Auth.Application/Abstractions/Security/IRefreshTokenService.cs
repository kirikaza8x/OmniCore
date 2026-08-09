namespace OmniCore.Services.Auth.Application.Abstractions.Security;

public interface IRefreshTokenService
{
    int RefreshTokenExpiryDays { get; }

    /// <summary>
    /// Generates a cryptographically secure random refresh token string and its validity duration.
    /// </summary>
    (string Token, TimeSpan Duration) GenerateRefreshToken();
}