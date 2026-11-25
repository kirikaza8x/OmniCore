namespace OmniCore.Services.Auth.Domain.Repositories;

using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

/// <summary>An account's membership with the tenant's slug and name (active tenants only).</summary>
public sealed record TenantMembershipInfo(TenantId TenantId, string Slug, string Name, string Role, DateTime JoinedAtUtc);

public interface ITenantRepository
{
    /// <summary>The account's memberships of <b>active</b> tenants, oldest first.</summary>
    Task<IReadOnlyList<TenantMembershipInfo>> GetMembershipsAsync(AccountId accountId, CancellationToken cancellationToken = default);

    /// <summary>The (tracked) membership, active tenant or not.</summary>
    Task<TenantMembership?> GetMembershipAsync(TenantId tenantId, AccountId accountId, CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    void Add(Tenant tenant);

    void Add(TenantMembership membership);
}
