namespace OmniCore.Services.Auth.Application.Abstractions.Security;

using System.Security.Claims;

public interface IJwtTokenService
{
    int ExpiryMinutes { get; }
    int RefreshTokenExpiryDays { get; }
    
    /// <param name="tenant">The active tenant (claims <c>tenant_id</c>, <c>tenant_role</c>); null issues a token without one.</param>
    string GenerateToken(Guid userId, string? email, string? name, IEnumerable<string> roles, TenantClaim? tenant = null);
    ClaimsPrincipal? ValidateToken(string token, bool allowExpired = false);
    string GenerateRefreshToken();
    bool IsTokenExpired(string token);
    int GetMinutesUntilExpiry(string token);
}