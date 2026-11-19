namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Login;

using FluentValidation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Username or Email is required.")
            .MaximumLength(256).WithMessage("Email or username is too long.");

        // bcrypt only uses the first 72 bytes; the cap also stops huge bodies being hashed.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password is too long.");
    }
}
