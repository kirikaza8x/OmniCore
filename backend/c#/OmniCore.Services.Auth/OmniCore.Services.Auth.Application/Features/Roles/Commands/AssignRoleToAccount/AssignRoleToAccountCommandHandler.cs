namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.AssignRoleToAccount;

using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class AssignRoleToAccountCommandHandler(
    IAccountRepository accountRepository,
    IRoleRepository roleRepository) : ICommandHandler<AssignRoleToAccountCommand>
{
    public async Task<Result> Handle(
        AssignRoleToAccountCommand request, 
        CancellationToken cancellationToken)
    {
        var roleId = new RoleId(request.RoleId);
        var roleExists = await roleRepository.ExistsAsync(roleId, cancellationToken);
        if (!roleExists)
        {
            return Result.Failure(
                Error.NotFound("Role.NotFound", $"Role with ID '{request.RoleId}' was not found."));
        }

        var accountId = new AccountId(request.AccountId);
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure(
                Error.NotFound("Account.NotFound", $"Account with ID '{request.AccountId}' was not found."));
        }

        return account.AssignRole(roleId);
    }
}