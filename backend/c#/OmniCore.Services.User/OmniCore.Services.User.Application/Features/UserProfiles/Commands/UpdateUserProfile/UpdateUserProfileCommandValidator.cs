// Features/UserProfiles/Commands/UpdateUserProfile/UpdateUserProfileCommandValidator.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.UpdateUserProfile;

using FluentValidation;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters.");
    }
}