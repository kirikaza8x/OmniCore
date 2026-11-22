namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMySettings;

using FluentValidation;

public sealed class UpdateMySettingsCommandValidator : AbstractValidator<UpdateMySettingsCommand>
{
    public UpdateMySettingsCommandValidator()
    {
        RuleFor(x => x.Theme).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
    }
}
