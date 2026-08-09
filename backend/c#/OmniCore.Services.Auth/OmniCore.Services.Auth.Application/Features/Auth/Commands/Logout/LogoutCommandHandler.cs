namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Logout;

using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record LogoutCommand(
    string RefreshToken) : ICommand;

public sealed class LogoutCommandHandler(
    IAccountRepository accountRepository) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(
        LogoutCommand request, 
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (account is null)
        {
            // Idempotent success if token/account does not exist
            return Result.Success();
        }

        // Revoke token via Aggregate Root method
        account.RevokeRefreshToken(request.RefreshToken);

        // Unit of Work automatically commits revocation state on Result.Success
        return Result.Success();
    }
}