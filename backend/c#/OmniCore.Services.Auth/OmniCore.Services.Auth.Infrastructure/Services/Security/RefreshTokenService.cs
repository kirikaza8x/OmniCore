namespace OmniCore.Services.Auth.Infrastructure.Services.Security;

using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Configs;

public sealed class RefreshTokenService(
    IOptions<JwtConfig> options) : IRefreshTokenService
{
    public int RefreshTokenExpiryDays => options.Value.RefreshTokenExpiryDays;

    public RefreshToken GenerateToken(Guid userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var result = RefreshToken.Create(
            accountId: AccountId.New(),
            token: token,
            duration: TimeSpan.FromDays(RefreshTokenExpiryDays)
        );

        return result.Value;
    }

    public bool ValidateToken(RefreshToken token)
    {
        return token is not null && token.IsActive;
    }

    public void RevokeToken(RefreshToken token)
    {
        token?.Revoke();
    }
}