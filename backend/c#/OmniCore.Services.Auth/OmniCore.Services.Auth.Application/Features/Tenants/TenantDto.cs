namespace OmniCore.Services.Auth.Application.Features.Tenants;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;

/// <param name="IsCurrent">True for the tenant of the token that made the call (only set by the list query).</param>
public record TenantDto(Guid Id, string Slug, string Name, string Role, bool IsCurrent = false);

/// <summary>Chooses the tenant a new access token is issued for (ADR 0001, section 3).</summary>
public static class TenantSelector
{
    /// <summary>The preferred tenant when the account is a member of it, else the default tenant, else the oldest membership.</summary>
    public static TenantMembershipInfo? Choose(IReadOnlyList<TenantMembershipInfo> memberships, Guid? preferred = null)
    {
        if (memberships.Count == 0)
        {
            return null;
        }

        if (preferred is { } wanted)
        {
            var match = memberships.FirstOrDefault(m => m.TenantId.Value == wanted);
            if (match is not null)
            {
                return match;
            }
        }

        return memberships.FirstOrDefault(m => m.TenantId == Tenant.DefaultId) ?? memberships[0];
    }

    public static TenantClaim? ToClaim(TenantMembershipInfo? membership) =>
        membership is null ? null : new TenantClaim(membership.TenantId.Value, membership.Role);
}
