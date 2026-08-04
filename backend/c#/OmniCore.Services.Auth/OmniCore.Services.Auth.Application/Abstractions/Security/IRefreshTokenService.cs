namespace OmniCore.Services.Auth.Application.Abstractions.Security;

using OmniCore.Services.Auth.Domain.Entities;

public interface IRefreshTokenService
{
    int RefreshTokenExpiryDays { get; }
    RefreshToken GenerateToken(Guid userId);
    bool ValidateToken(RefreshToken token);
    void RevokeToken(RefreshToken token);
}