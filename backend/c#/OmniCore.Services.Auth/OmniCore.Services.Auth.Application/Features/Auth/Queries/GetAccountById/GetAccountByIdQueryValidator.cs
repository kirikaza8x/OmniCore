namespace OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;

using FluentValidation;

public sealed class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    public GetAccountByIdQueryValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}