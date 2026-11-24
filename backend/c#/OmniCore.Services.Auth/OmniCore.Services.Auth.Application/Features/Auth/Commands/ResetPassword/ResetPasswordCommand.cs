namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.ResetPassword;

using FluentValidation;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommand;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.")
            .MaximumLength(128).WithMessage("Token is invalid.");

        // Same rules as registration.
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[\^$*.[\]{}()?""!@#%&/\\,><':;|_~`]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class ResetPasswordCommandHandler(
    IAccountRepository accountRepository,
    IPasswordHasherService passwordHasher) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var codeHash = SecurityToken.HashCode(request.Token);
        var account = await accountRepository.GetBySecurityTokenAsync(
            codeHash, SecurityTokenType.PasswordReset, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "The link is invalid or has expired."));
        }

        var redeemed = account.RedeemSecurityToken(SecurityTokenType.PasswordReset, codeHash);
        if (redeemed.IsFailure)
        {
            return redeemed;
        }

        // Also revokes every refresh token of the account.
        return account.UpdatePassword(passwordHasher.HashPassword(request.NewPassword), "PasswordReset");
    }
}
