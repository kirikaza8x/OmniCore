
namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.AssignRoleToAccount;
using FluentValidation;

public sealed class AssignRoleToAccountCommandValidator : AbstractValidator<AssignRoleToAccountCommand>
{
    public AssignRoleToAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}