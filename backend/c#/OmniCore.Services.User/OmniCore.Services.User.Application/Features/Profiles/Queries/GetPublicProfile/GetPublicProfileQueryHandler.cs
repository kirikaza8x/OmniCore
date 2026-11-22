namespace OmniCore.Services.User.Application.Features.Profiles.Queries.GetPublicProfile;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetPublicProfileQueryHandler(IUserProfileRepository profiles)
    : IQueryHandler<GetPublicProfileQuery, PublicProfileResponse>
{
    public async Task<Result<PublicProfileResponse>> Handle(GetPublicProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetReadOnlyAsync(UserId.From(request.UserId), cancellationToken);
        return profile is null || !profile.IsActive
            ? Result.Failure<PublicProfileResponse>(UserProfileErrors.NotFound)
            : profile.ToPublicResponse();
    }
}
