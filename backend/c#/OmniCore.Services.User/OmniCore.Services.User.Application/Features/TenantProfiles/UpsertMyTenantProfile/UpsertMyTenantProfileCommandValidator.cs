namespace OmniCore.Services.User.Application.Features.TenantProfiles.UpsertMyTenantProfile;

using FluentValidation;
using OmniCore.Services.User.Domain.Entities;

public sealed class UpsertMyTenantProfileCommandValidator : AbstractValidator<UpsertMyTenantProfileCommand>
{
    public UpsertMyTenantProfileCommandValidator()
    {
        RuleFor(x => x.GamerTag).NotEmpty().MaximumLength(UserTenantProfile.MaxGamerTagLength);
        RuleFor(x => x.CustomAvatarUrl).MaximumLength(UserTenantProfile.MaxAvatarUrlLength);
    }
}
