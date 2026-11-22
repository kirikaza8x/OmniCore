namespace OmniCore.Services.User.Application.Features.Profiles.Queries.GetProfileDetails;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetProfileDetailsQueryHandler(IUserProfileRepository profiles)
    : IQueryHandler<GetProfileDetailsQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(GetProfileDetailsQuery request, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetReadOnlyAsync(UserId.From(request.UserId), cancellationToken);
        return profile is null
            ? Result.Failure<UserProfileResponse>(UserProfileErrors.NotFound)
            : profile.ToResponse();
    }
}
