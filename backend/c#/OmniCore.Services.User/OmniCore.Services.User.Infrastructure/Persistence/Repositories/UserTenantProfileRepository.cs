namespace OmniCore.Services.User.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Services.User.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class UserTenantProfileRepository(UserDbContext dbContext)
    : RepositoryBase<UserTenantProfile, UserTenantProfileId>(dbContext), IUserTenantProfileRepository
{
    public Task<UserTenantProfile?> GetForUserAsync(UserId userId, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
