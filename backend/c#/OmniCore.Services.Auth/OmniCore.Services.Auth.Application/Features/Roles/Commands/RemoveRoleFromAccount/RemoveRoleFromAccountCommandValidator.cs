namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.RemoveRoleFromAccount;

using FluentValidation;

public sealed class RemoveRoleFromAccountCommandValidator : AbstractValidator<RemoveRoleFromAccountCommand>
{
    public RemoveRoleFromAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}

