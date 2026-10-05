// Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommandHandler.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.CreateUserProfile;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Services.User.Application.Features.UserProfiles.Mappings;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class CreateUserProfileCommandHandler(
    IUserProfileRepository userProfileRepository) : ICommandHandler<CreateUserProfileCommand, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        CreateUserProfileCommand request, 
        CancellationToken cancellationToken)
    {
        var (isEmailTaken, isUsernameTaken) = await userProfileRepository.CheckUniquenessAsync(
            request.Email, 
            request.Username, 
            cancellationToken);

        if (isUsernameTaken)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.UsernameAlreadyExists);
        }

        if (isEmailTaken)
        {
            return Result.Failure<UserProfileResponse>(UserProfileErrors.EmailAlreadyExists);
        }

        var profileResult = UserProfile.Create(
            request.UserId,
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName);

        if (profileResult.IsFailure)
        {
            return Result.Failure<UserProfileResponse>(profileResult.Error);
        }

        userProfileRepository.Add(profileResult.Value);

        return Result.Success(profileResult.Value.ToResponse());
    }
}