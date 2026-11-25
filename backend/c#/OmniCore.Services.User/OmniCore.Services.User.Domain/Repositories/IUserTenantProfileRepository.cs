namespace OmniCore.Services.User.Domain.Repositories;

using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;

/// <summary>Every query is already limited to the active tenant by the EF filter (ADR 0001).</summary>
public interface IUserTenantProfileRepository : IRepository<UserTenantProfile, UserTenantProfileId>
{
    /// <summary>The caller's profile in the active tenant, tracked. Null when none exists yet.</summary>
    Task<UserTenantProfile?> GetForUserAsync(UserId userId, CancellationToken cancellationToken = default);
}
