namespace OmniCore.Services.User.Domain.Repositories;

using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;

/// <summary>
/// <c>GetByIdAsync</c> (from the base interface) returns a tracked profile with settings and addresses, for changes.
/// <see cref="GetReadOnlyAsync"/> returns the same data untracked, for queries.
/// Deleted profiles are never returned.
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile, UserId>
{
    Task<UserProfile?> GetReadOnlyAsync(UserId id, CancellationToken cancellationToken = default);
}
