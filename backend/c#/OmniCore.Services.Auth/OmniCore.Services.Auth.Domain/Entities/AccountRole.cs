using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Many-to-Many junction entity linking Accounts to assigned Roles.
/// </summary>
public class AccountRole
{
    public AccountId AccountId { get; private set; } = null!;
    public RoleId RoleId { get; private set; } = null!;
    public DateTime AssignedAtUtc { get; private set; }

    public Account Account { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private AccountRole() { }

    private AccountRole(AccountId accountId, RoleId roleId)
    {
        AccountId = accountId;
        RoleId = roleId;
        AssignedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Binds a Role to an Account.
    /// </summary>
    /// <param name="accountId">The target account identifier.</param>
    /// <param name="roleId">The role identifier to assign.</param>
    /// <returns>A <see cref="Result{AccountRole}"/> junction entity.</returns>
    public static Result<AccountRole> Create(AccountId accountId, RoleId roleId)
    {
        if (accountId is null)
        {
            return Result.Failure<AccountRole>(Error.Validation("AccountRole.AccountIdRequired", "Account ID is required."));
        }

        if (roleId is null)
        {
            return Result.Failure<AccountRole>(Error.Validation("AccountRole.RoleIdRequired", "Role ID is required."));
        }

        return new AccountRole(accountId, roleId);
    }
}