namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.Events;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;

public partial class Account
{
    /// <summary>
    /// Assigns a role to the account if it has not already been assigned.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to assign.</param>
    /// <returns>A result indicating success or a conflict error if the role is already assigned.</returns>
    public Result AssignRole(RoleId roleId)
    {
        if (_accountRoles.Any(ar => ar.RoleId == roleId))
        {
            return Result.Failure(
                Error.Conflict("Account.RoleAlreadyAssigned", "This role is already assigned to the account."));
        }

        var accountRoleResult = AccountRole.Create(Id, roleId);
        if (accountRoleResult.IsFailure)
        {
            return Result.Failure(accountRoleResult.Error);
        }

        _accountRoles.Add(accountRoleResult.Value);
        ModifiedAt = DateTime.UtcNow;

        // Uses RaiseDomainEvent from AggregateRoot<TId>
        RaiseDomainEvent(new AccountRoleAssignedDomainEvent(Id, roleId));

        return Result.Success();
    }

    /// <summary>
    /// Removes an assigned role from the account.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to remove.</param>
    /// <returns>A result indicating success or a not found error if the role was not assigned.</returns>
    public Result RemoveRole(RoleId roleId)
    {
        var accountRole = _accountRoles.FirstOrDefault(ar => ar.RoleId == roleId);

        if (accountRole is null)
        {
            return Result.Failure(
                Error.NotFound("Account.RoleNotAssigned", "The specified role is not assigned to this account."));
        }

        _accountRoles.Remove(accountRole);
        ModifiedAt = DateTime.UtcNow;

        // Uses RaiseDomainEvent from AggregateRoot<TId>
        RaiseDomainEvent(new AccountRoleRemovedDomainEvent(Id, roleId));

        return Result.Success();
    }
}