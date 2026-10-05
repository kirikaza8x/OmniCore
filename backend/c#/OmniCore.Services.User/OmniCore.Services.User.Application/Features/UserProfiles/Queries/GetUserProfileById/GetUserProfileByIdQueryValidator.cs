// Features/UserProfiles/Queries/GetUserProfileById/GetUserProfileByIdQueryValidator.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Queries.GetUserProfileById;

using FluentValidation;

public sealed class GetUserProfileByIdQueryValidator : AbstractValidator<GetUserProfileByIdQuery>
{
    public GetUserProfileByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}