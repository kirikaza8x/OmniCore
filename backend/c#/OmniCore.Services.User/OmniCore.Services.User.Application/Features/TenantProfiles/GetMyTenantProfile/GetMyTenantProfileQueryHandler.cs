namespace OmniCore.Services.User.Application.Features.TenantProfiles.GetMyTenantProfile;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetMyTenantProfileQueryHandler(IUserTenantProfileRepository profiles, ICurrentUserService currentUser)
    : IQueryHandler<GetMyTenantProfileQuery, TenantProfileResponse>
{
    public async Task<Result<TenantProfileResponse>> Handle(GetMyTenantProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<TenantProfileResponse>(userId.Error);

        var profile = await profiles.GetForUserAsync(userId.Value, cancellationToken);
        return profile is null
            ? Result.Failure<TenantProfileResponse>(UserProfileErrors.TenantProfileNotFound)
            : profile.ToResponse();
    }
}
