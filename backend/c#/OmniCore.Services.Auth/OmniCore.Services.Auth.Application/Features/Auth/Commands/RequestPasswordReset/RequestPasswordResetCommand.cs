namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.RequestPasswordReset;

using FluentValidation;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record RequestPasswordResetCommand(string Email) : ICommand;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");
    }
}

/// <summary>
/// Always succeeds, whether or not the e-mail belongs to an account, so the endpoint cannot be used to find out
/// which e-mails are registered (ADR 0009).
/// </summary>
public sealed class RequestPasswordResetCommandHandler(
    IAccountRepository accountRepository,
    IEventBus eventBus) : ICommandHandler<RequestPasswordResetCommand>
{
    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByEmailWithSecurityTokensAsync(request.Email, cancellationToken);
        if (account is null || !account.IsActive || account.Email is null)
        {
            return Result.Success();
        }

        var code = SecurityToken.GenerateCode();
        var issued = account.IssueSecurityToken(
            SecurityTokenType.PasswordReset,
            SecurityToken.HashCode(code),
            SecurityTokenLifetimes.PasswordReset);
        if (issued.IsFailure)
        {
            return Result.Success();
        }

        await eventBus.PublishAsync(
            new PasswordResetRequestedIntegrationEvent(
                account.Id.Value,
                account.Username.Value,
                account.Email.Value,
                code,
                (int)SecurityTokenLifetimes.PasswordReset.TotalMinutes),
            cancellationToken);

        return Result.Success();
    }
}
