namespace OmniCore.Services.Auth.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class AccountRepository(AuthDbContext dbContext) 
    : RepositoryBase<Account, AccountId>(dbContext), IAccountRepository
{
    public async Task<Account?> GetByUsernameAsync(
        string username, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.AccountRoles)
            .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Username.Value == username, cancellationToken);
    }

    public async Task<Account?> GetByEmailAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.AccountRoles)
            .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Email != null && a.Email.Value == email, cancellationToken);
    }

    public async Task<Account?> GetByIdentifierAsync(
        string identifier, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.AccountRoles)
            .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(
                a => a.Username.Value == identifier || 
                     (a.Email != null && a.Email.Value == identifier), 
                cancellationToken);
    }

    public async Task<bool> IsUsernameUniqueAsync(
        string username, 
        CancellationToken cancellationToken = default)
    {
        return !await DbSet.AnyAsync(a => a.Username.Value == username, cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        return !await DbSet.AnyAsync(a => a.Email != null && a.Email.Value == email, cancellationToken);
    }

    public async Task<(bool IsEmailTaken, bool IsUsernameTaken)> CheckUniquenessAsync(
    string email, 
    string username, 
    CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .AsNoTracking()
            .Where(a => a.Username.Value == username || (a.Email != null && a.Email.Value == email))
            .Select(a => new { 
                IsEmail = a.Email != null && a.Email.Value == email, 
                IsUsername = a.Username.Value == username 
            })
            .ToListAsync(cancellationToken);

        bool isEmailTaken = existing.Any(x => x.IsEmail);
        bool isUsernameTaken = existing.Any(x => x.IsUsername);

        return (isEmailTaken, isUsernameTaken);
    }
}