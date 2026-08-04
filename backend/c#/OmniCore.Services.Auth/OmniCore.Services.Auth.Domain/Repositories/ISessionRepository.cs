namespace OmniCore.Services.Auth.Domain.Repositories;

using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;

public interface ISessionRepository : IRepository<UserSession, UserSessionId>
{
    Task<UserSession?> GetByRefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSession>> GetActiveSessionsByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default);
    Task RevokeAllAccountSessionsAsync(AccountId accountId, CancellationToken cancellationToken = default);
}