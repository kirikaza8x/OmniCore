namespace OmniCore.Services.Auth.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public sealed class RoleRepository(AuthDbContext dbContext) 
    : RepositoryBase<Role, RoleId>(dbContext), IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        return await DbSet.FirstOrDefaultAsync(r => r.Name.ToUpper() == normalizedName, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        return !await DbSet.AnyAsync(r => r.Name.ToUpper() == normalizedName, cancellationToken);
    }
}