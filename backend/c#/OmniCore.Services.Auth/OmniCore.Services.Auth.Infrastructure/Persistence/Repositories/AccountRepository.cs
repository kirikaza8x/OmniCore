namespace OmniCore.Services.Auth.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Domain.ValueObjects;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class AccountRepository(AuthDbContext dbContext)
    : RepositoryBase<Account, AccountId>(dbContext), IAccountRepository
{
    /// <summary>
    /// Overridden to eagerly load RefreshTokens, AccountRoles, and Roles for aggregate domain actions and claims generation.
    /// </summary>
    public override async Task<Account?> GetByIdAsync(
        AccountId id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.RefreshTokens)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var usernameResult = Username.Create(username);
        if (usernameResult.IsFailure)
        {
            return null;
        }

        return await DbSet
            .Include(a => a.RefreshTokens)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Username == usernameResult.Value, cancellationToken);
    }

    public async Task<Account?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var emailResult = EmailAddress.Create(email);
        if (emailResult.IsFailure)
        {
            return null;
        }

        return await DbSet
            .Include(a => a.RefreshTokens)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Email == emailResult.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetByIdsAsync(
        IReadOnlyCollection<AccountId> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idList = ids.ToList();
        return await DbSet.Where(a => idList.Contains(a.Id)).ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetByEmailWithSecurityTokensAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var emailResult = EmailAddress.Create(email);
        if (emailResult.IsFailure)
        {
            return null;
        }

        return await DbSet
            .Include(a => a.SecurityTokens)
            .FirstOrDefaultAsync(a => a.Email == emailResult.Value, cancellationToken);
    }

    public async Task<Account?> GetByIdWithSecurityTokensAsync(
        AccountId id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.SecurityTokens)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetBySecurityTokenAsync(
        string codeHash,
        SecurityTokenType type,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
        {
            return null;
        }

        // Refresh tokens are loaded too: a password reset revokes every session.
        return await DbSet
            .Include(a => a.SecurityTokens)
            .Include(a => a.RefreshTokens)
            .FirstOrDefaultAsync(
                a => a.SecurityTokens.Any(t => t.CodeHash == codeHash && t.TokenType == type),
                cancellationToken);
    }

    public async Task<Account?> GetByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var usernameResult = Username.Create(identifier);
        var emailResult = EmailAddress.Create(identifier);

        var usernameVo = usernameResult.IsSuccess ? usernameResult.Value : null;
        var emailVo = emailResult.IsSuccess ? emailResult.Value : null;

        if (usernameVo is null && emailVo is null)
        {
            return null;
        }

        return await DbSet
            .Include(a => a.RefreshTokens)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(
                a => (usernameVo != null && a.Username == usernameVo)
                  || (emailVo != null && a.Email == emailVo),
                cancellationToken);
    }

    public async Task<Account?> GetByRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        // Only hashes are stored, so look the token up by its hash (ADR 0003).
        var tokenHash = RefreshToken.HashToken(refreshToken);

        return await DbSet
            .Include(a => a.RefreshTokens)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.RefreshTokens.Any(rt => rt.TokenHash == tokenHash), cancellationToken);
    }

    public async Task<bool> IsUsernameUniqueAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var usernameResult = Username.Create(username);
        if (usernameResult.IsFailure)
        {
            return true;
        }

        return !await DbSet.AnyAsync(a => a.Username == usernameResult.Value, cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var emailResult = EmailAddress.Create(email);
        if (emailResult.IsFailure)
        {
            return true;
        }

        return !await DbSet.AnyAsync(a => a.Email == emailResult.Value, cancellationToken);
    }

    public async Task<(bool IsEmailTaken, bool IsUsernameTaken)> CheckUniquenessAsync(
        string email,
        string username,
        CancellationToken cancellationToken = default)
    {
        var usernameResult = Username.Create(username);
        var emailResult = EmailAddress.Create(email);

        var usernameVo = usernameResult.IsSuccess ? usernameResult.Value : null;
        var emailVo = emailResult.IsSuccess ? emailResult.Value : null;

        if (usernameVo is null && emailVo is null)
        {
            return (false, false);
        }

        var existing = await DbSet
            .AsNoTracking()
            .Where(a => (usernameVo != null && a.Username == usernameVo)
                     || (emailVo != null && a.Email == emailVo))
            .Select(a => new
            {
                IsUsername = usernameVo != null && a.Username == usernameVo,
                IsEmail = emailVo != null && a.Email == emailVo
            })
            .ToListAsync(cancellationToken);

        bool isUsernameTaken = existing.Any(x => x.IsUsername);
        bool isEmailTaken = existing.Any(x => x.IsEmail);

        return (isEmailTaken, isUsernameTaken);
    }

    public Account? GetLocalById(AccountId accountId)
    {
        return dbContext.Accounts.Local.FirstOrDefault(a => a.Id == accountId);
    }
}