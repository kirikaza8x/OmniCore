// Features/UserSettings/Commands/UpdateUserSettings/UpdateUserSettingsCommandValidator.cs
namespace OmniCore.Services.User.Application.Features.UserSettings.Commands.UpdateUserSettings;

using FluentValidation;

public sealed class UpdateUserSettingsCommandValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    public UpdateUserSettingsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Theme)
            .NotEmpty().WithMessage("Theme is required.")
            .MaximumLength(20).WithMessage("Theme cannot exceed 20 characters.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required.")
            .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters.");
    }
}