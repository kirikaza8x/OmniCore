namespace OmniCore.Services.Auth.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class RoleRepository(AuthDbContext dbContext)
    : RepositoryBase<Role, RoleId>(dbContext), IRoleRepository
{
    public async Task<Role?> GetByNameAsync(
        string name, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmedName = name.Trim().ToLower();

        return await DbSet
            .FirstOrDefaultAsync(r => r.Name.ToLower() == trimmedName, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(
        string name, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var trimmedName = name.Trim().ToLower();

        return !await DbSet
            .AnyAsync(r => r.Name.ToLower() == trimmedName, cancellationToken);
    }
}