// Features/UserProfiles/Queries/GetUserProfileById/GetUserProfileByIdQueryHandler.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Queries.GetUserProfileById;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Services.User.Application.Features.UserProfiles.Mappings;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.Specifications;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetUserProfileByIdQueryHandler(
    IUserProfileRepository userProfileRepository) : IQueryHandler<GetUserProfileByIdQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetUserProfileByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var spec = new UserProfileFullSpecification(userId);
        var profile = await userProfileRepository.GetWithSpecificationAsync(spec, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.NotFound);
        }

        return Result.Success(profile.ToResponse());
    }
}