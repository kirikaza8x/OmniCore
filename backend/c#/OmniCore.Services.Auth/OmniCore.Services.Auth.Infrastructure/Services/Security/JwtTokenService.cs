namespace OmniCore.Services.Auth.Infrastructure.Services.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Infrastructure.Configs.Security;
using OmniCore.Shared.Infrastructure.Security;

public sealed class JwtTokenService(
    IOptions<JwtConfig> options,
    JwtKeySet keySet,
    IDateTimeProvider dateTimeProvider,
    ILogger<JwtTokenService> logger) : IJwtTokenService
{
    private readonly JwtConfig _config = options.Value;

    public int ExpiryMinutes => _config.ExpiryMinutes;
    public int RefreshTokenExpiryDays => _config.RefreshTokenExpiryDays;

    public string GenerateToken(
        Guid userId,
        string? email,
        string? name,
        IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));

        if (!string.IsNullOrWhiteSpace(name))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, name));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = dateTimeProvider.UtcNow.AddMinutes(ExpiryMinutes),
            Issuer = _config.Issuer,
            Audience = _config.Audience,
            // RS256 with the private key when configured (ADR 0003 step 4); the legacy HS256 secret otherwise.
            SigningCredentials = keySet.Signing is not null
                ? new SigningCredentials(keySet.Signing, SecurityAlgorithms.RsaSha256)
                : new SigningCredentials(
                    keySet.Hs256Key ?? throw new InvalidOperationException("No JWT signing key is configured."),
                    SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool allowExpired = false)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = GetValidationParameters(allowExpired);

            return tokenHandler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "JWT validation failed.");
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public bool IsTokenExpired(string token) => GetMinutesUntilExpiry(token) <= 0;

    public int GetMinutesUntilExpiry(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return (int)(jwt.ValidTo - dateTimeProvider.UtcNow).TotalMinutes;
        }
        catch
        {
            return -1;
        }
    }

    private TokenValidationParameters GetValidationParameters(bool allowExpired)
    {
        return new TokenValidationParameters
        {
            ValidAlgorithms = keySet.ValidAlgorithms,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, _) => keySet.ResolveKeys(kid),

            ValidateIssuer = true,
            ValidIssuer = _config.Issuer,

            ValidateAudience = true,
            ValidAudience = _config.Audience,

            ValidateLifetime = !allowExpired,
            ClockSkew = TimeSpan.Zero
        };
    }
}