namespace OmniCore.Services.Auth.Application.Abstractions.Security;

using System.Security.Claims;

public interface IJwtTokenService
{
    int ExpiryMinutes { get; }
    int RefreshTokenExpiryDays { get; }
    
    string GenerateToken(Guid userId, string? email, string? name, IEnumerable<string> roles);
    ClaimsPrincipal? ValidateToken(string token, bool allowExpired = false);
    string GenerateRefreshToken();
    bool IsTokenExpired(string token);
    int GetMinutesUntilExpiry(string token);
}