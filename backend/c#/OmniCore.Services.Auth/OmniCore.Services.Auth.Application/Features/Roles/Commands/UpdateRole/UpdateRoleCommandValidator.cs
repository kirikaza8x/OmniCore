
namespace OmniCore.Services.Auth.Application.Features.Roles.Commands.UpdateRole;

using FluentValidation;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
    }
}