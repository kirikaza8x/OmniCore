namespace OmniCore.Services.Auth.Domain.Repositories;

using OmniCore.Services.Auth.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}