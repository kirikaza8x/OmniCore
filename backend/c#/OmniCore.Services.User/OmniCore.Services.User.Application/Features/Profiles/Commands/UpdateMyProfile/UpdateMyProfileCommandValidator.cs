namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMyProfile;

using FluentValidation;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Bio).MaximumLength(500);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
    }
}
