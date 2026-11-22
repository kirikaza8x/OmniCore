namespace OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;

using FluentValidation;

public sealed class ProvisionUserProfileCommandValidator : AbstractValidator<ProvisionUserProfileCommand>
{
    public ProvisionUserProfileCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Username).NotEmpty().Length(3, 30);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}
