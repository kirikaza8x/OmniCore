namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.ResendEmailVerification;

using FluentValidation;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>For someone who cannot sign in because the e-mail is unverified (ADR 0010): asks for a new link by address.</summary>
public record ResendEmailVerificationCommand(string Email) : ICommand;

public sealed class ResendEmailVerificationCommandValidator : AbstractValidator<ResendEmailVerificationCommand>
{
    public ResendEmailVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");
    }
}

/// <summary>Always succeeds, whether or not a link was sent, so it cannot be used to find out which e-mails are registered.</summary>
public sealed class ResendEmailVerificationCommandHandler(
    IAccountRepository accountRepository,
    IEventBus eventBus) : ICommandHandler<ResendEmailVerificationCommand>
{
    public async Task<Result> Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByEmailWithSecurityTokensAsync(request.Email, cancellationToken);
        if (account is null || !account.IsActive || account.Email is null || account.IsEmailConfirmed)
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
            return Result.Success();
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
