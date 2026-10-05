// Features/UserTenantProfiles/Commands/CreateTenantProfile/CreateTenantProfileCommandValidator.cs
namespace OmniCore.Services.User.Application.Features.UserTenantProfiles.Commands.CreateTenantProfile;

using FluentValidation;

public sealed class CreateTenantProfileCommandValidator : AbstractValidator<CreateTenantProfileCommand>
{
    public CreateTenantProfileCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.GamerTag)
            .NotEmpty().WithMessage("GamerTag is required.")
            .Length(3, 30).WithMessage("GamerTag must be between 3 and 30 characters.");
    }
}