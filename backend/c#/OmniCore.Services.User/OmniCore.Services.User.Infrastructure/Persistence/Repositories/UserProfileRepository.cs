namespace OmniCore.Services.User.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Services.User.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class UserProfileRepository(UserDbContext dbContext)
    : RepositoryBase<UserProfile, UserId>(dbContext), IUserProfileRepository
{
    /// <summary>Tracked, with settings and addresses, for commands. Deleted profiles are not returned.</summary>
    public override Task<UserProfile?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(p => p.Settings)
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

    public Task<UserProfile?> GetReadOnlyAsync(UserId id, CancellationToken cancellationToken = default) =>
        DbSet
            .AsNoTracking()
            .Include(p => p.Settings)
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

    public override Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(p => p.Id == id, cancellationToken);
}
