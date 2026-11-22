namespace OmniCore.Services.User.Application.Features.Profiles.Queries.GetMyProfile;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetMyProfileQueryHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : IQueryHandler<GetMyProfileQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<UserProfileResponse>(userId.Error);

        var profile = await profiles.GetReadOnlyAsync(userId.Value, cancellationToken);
        return profile is null
            ? Result.Failure<UserProfileResponse>(UserProfileErrors.NotFound)
            : profile.ToResponse();
    }
}
