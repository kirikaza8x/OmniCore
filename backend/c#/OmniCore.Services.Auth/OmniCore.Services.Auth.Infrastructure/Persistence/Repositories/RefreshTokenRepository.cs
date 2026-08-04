namespace OmniCore.Services.Auth.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;

public sealed class RefreshTokenRepository(AuthDbContext dbContext) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<RefreshToken>().AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }
}