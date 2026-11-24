namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.ConfirmEmail;

using FluentValidation;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record ConfirmEmailCommand(string Token) : ICommand;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(128).WithMessage("Token is invalid.");
    }
}

public sealed class ConfirmEmailCommandHandler(IAccountRepository accountRepository) : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var codeHash = SecurityToken.HashCode(request.Token);
        var account = await accountRepository.GetBySecurityTokenAsync(
            codeHash, SecurityTokenType.EmailVerification, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "The link is invalid or has expired."));
        }

        var redeemed = account.RedeemSecurityToken(SecurityTokenType.EmailVerification, codeHash);
        if (redeemed.IsFailure)
        {
            return redeemed;
        }

        return account.ConfirmEmail();
    }
}
