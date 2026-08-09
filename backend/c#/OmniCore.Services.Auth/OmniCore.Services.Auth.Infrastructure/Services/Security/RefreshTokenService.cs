namespace OmniCore.Services.Auth.Infrastructure.Services.Security;

using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Shared.Infrastructure.Configs.Security;

public sealed class RefreshTokenService(
    IOptions<JwtConfig> options) : IRefreshTokenService
{
    public int RefreshTokenExpiryDays => options.Value.RefreshTokenExpiryDays;

    public (string Token, TimeSpan Duration) GenerateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var duration = TimeSpan.FromDays(RefreshTokenExpiryDays);

        return (token, duration);
    }
}