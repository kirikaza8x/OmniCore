namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.RemoveRoleFromAccount;

using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class RemoveRoleFromAccountCommandHandler(
    IAccountRepository accountRepository) : ICommandHandler<RemoveRoleFromAccountCommand>
{
    public async Task<Result> Handle(
        RemoveRoleFromAccountCommand request, 
        CancellationToken cancellationToken)
    {
        var accountId = new AccountId(request.AccountId);
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure(
                Error.NotFound("Account.NotFound", $"Account with ID '{request.AccountId}' was not found."));
        }

        var roleId = new RoleId(request.RoleId);
        return account.RemoveRole(roleId);
    }
}