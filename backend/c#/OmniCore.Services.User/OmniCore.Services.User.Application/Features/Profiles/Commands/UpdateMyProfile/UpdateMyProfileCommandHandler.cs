namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMyProfile;

using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpdateMyProfileCommandHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : ICommandHandler<UpdateMyProfileCommand, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<UserProfileResponse>(userId.Error);

        var profile = await profiles.GetByIdAsync(userId.Value, cancellationToken);
        if (profile is null) return Result.Failure<UserProfileResponse>(UserProfileErrors.NotFound);

        var updated = profile.UpdatePersonalInformation(request.FirstName, request.LastName, request.Bio, request.PhoneNumber);
        if (updated.IsFailure) return Result.Failure<UserProfileResponse>(updated.Error);

        // The profile was loaded with tracking, so the unit-of-work behaviour saves the change (no Update() call needed).
        return profile.ToResponse();
    }
}
