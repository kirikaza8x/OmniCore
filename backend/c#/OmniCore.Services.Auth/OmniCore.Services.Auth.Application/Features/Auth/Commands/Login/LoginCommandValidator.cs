namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Login;

using FluentValidation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Username or Email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}