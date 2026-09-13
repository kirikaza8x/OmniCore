// Features/UserProfiles/Commands/UpdateUserProfile/UpdateUserProfileCommandHandler.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.UpdateUserProfile;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Services.User.Application.Features.UserProfiles.Mappings;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpdateUserProfileCommandHandler(
    IUserProfileRepository userProfileRepository) : ICommandHandler<UpdateUserProfileCommand, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        UpdateUserProfileCommand request, 
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var profile = await userProfileRepository.GetByIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.NotFound);
        }

        var updateResult = profile.UpdatePersonalInformation(
            request.FirstName, 
            request.LastName, 
            request.Bio, 
            request.PhoneNumber);

        if (updateResult.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(updateResult.Error);
        }

        userProfileRepository.Update(profile);

        return Result.Success(profile.ToResponse());
    }
}