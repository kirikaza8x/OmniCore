namespace OmniCore.Services.Auth.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class SessionRepository(AuthDbContext dbContext) 
    : RepositoryBase<UserSession, UserSessionId>(dbContext), ISessionRepository
{
    public async Task<UserSession?> GetByRefreshTokenAsync(
        string refreshTokenValue, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.RefreshToken)
            .FirstOrDefaultAsync(
                s => s.RefreshToken != null && 
                     s.RefreshToken.Token == refreshTokenValue && 
                     !s.IsRevoked, 
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsByAccountIdAsync(
        AccountId accountId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.AccountId == accountId && !s.IsRevoked)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllAccountSessionsAsync(
        AccountId accountId, 
        CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(s => s.AccountId == accountId && !s.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsRevoked, true), cancellationToken);
    }
}