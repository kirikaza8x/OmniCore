namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

/// <summary>Tenant roles (ADR 0001). Platform roles (SuperAdmin) stay on the account.</summary>
public static class TenantRoles
{
    public const string Owner = "TenantOwner";
    public const string Admin = "TenantAdmin";
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All = [Owner, Admin, Member];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}

/// <summary>An account's membership of one tenant, with its role there. Key: (tenant, account).</summary>
public class TenantMembership
{
    public TenantId TenantId { get; private set; }
    public AccountId AccountId { get; private set; } = null!;
    public string Role { get; private set; } = TenantRoles.Member;
    public DateTime JoinedAtUtc { get; private set; }

    private TenantMembership() { }

    private TenantMembership(TenantId tenantId, AccountId accountId, string role, DateTime joinedAtUtc)
    {
        TenantId = tenantId;
        AccountId = accountId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    public static Result<TenantMembership> Create(TenantId tenantId, AccountId accountId, string role, DateTime utcNow)
    {
        if (accountId is null)
        {
            return Result.Failure<TenantMembership>(Error.Validation("TenantMembership.AccountRequired", "Account is required."));
        }

        if (!TenantRoles.IsValid(role))
        {
            return Result.Failure<TenantMembership>(Error.Validation(
                "TenantMembership.RoleInvalid", $"Role must be one of: {string.Join(", ", TenantRoles.All)}."));
        }

        return new TenantMembership(tenantId, accountId, role, utcNow);
    }

    public Result ChangeRole(string role)
    {
        if (!TenantRoles.IsValid(role))
        {
            return Result.Failure(Error.Validation(
                "TenantMembership.RoleInvalid", $"Role must be one of: {string.Join(", ", TenantRoles.All)}."));
        }

        Role = role;
        return Result.Success();
    }
}
