namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.RequestEmailVerification;

using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>Sends a verification link to the signed-in user's own e-mail. Does nothing if it is already verified.</summary>
public record RequestEmailVerificationCommand() : ICommand;

public sealed class RequestEmailVerificationCommandHandler(
    ICurrentUserService currentUser,
    IAccountRepository accountRepository,
    IEventBus eventBus) : ICommandHandler<RequestEmailVerificationCommand>
{
    public async Task<Result> Handle(RequestEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
        {
            return Result.Failure(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        var account = await accountRepository.GetByIdWithSecurityTokensAsync(new AccountId(userId.Value), cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
        }

        if (account.Email is null)
        {
            return Result.Failure(Error.Validation("Account.NoEmail", "The account has no e-mail address."));
        }

        if (account.IsEmailConfirmed)
        {
            return Result.Success();
        }

        var code = SecurityToken.GenerateCode();
        var issued = account.IssueSecurityToken(
            SecurityTokenType.EmailVerification,
            SecurityToken.HashCode(code),
            SecurityTokenLifetimes.EmailVerification);
        if (issued.IsFailure)
        {
            return Result.Failure(issued.Error);
        }

        await eventBus.PublishAsync(
            new EmailVerificationRequestedIntegrationEvent(
                account.Id.Value,
                account.Username.Value,
                account.Email.Value,
                code,
                (int)SecurityTokenLifetimes.EmailVerification.TotalMinutes),
            cancellationToken);

        return Result.Success();
    }
}
