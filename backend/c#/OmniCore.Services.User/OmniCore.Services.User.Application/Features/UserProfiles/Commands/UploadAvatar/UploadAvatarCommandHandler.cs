// Features/UserProfiles/Commands/UploadAvatar/UploadAvatarCommandHandler.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.UploadAvatar;

using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Storage;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UploadAvatarCommandHandler(
    IUserProfileRepository userProfileRepository,
    IStorageService storageService) : ICommandHandler<UploadAvatarCommand, string>
{
    public async Task<Result<string>> Handle(
        UploadAvatarCommand request, 
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var profile = await userProfileRepository.GetByIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<string>(UserProfileErrors.NotFound);
        }

        // Delete previous avatar file if exists
        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            try
            {
                await storageService.DeleteAsync(profile.AvatarUrl, cancellationToken);
            }
            catch
            {
                // Best-effort cleanup for old image
            }
        }

        var uploadResult = await storageService.UploadAsync(
            request.File, 
            folder: "avatars", 
            cancellationToken: cancellationToken);

        var avatarUpdateResult = profile.UpdateAvatar(uploadResult.PublicUrl);
        if (avatarUpdateResult.IsFailure)
        {
            return Result.Failure<string>(avatarUpdateResult.Error);
        }

        userProfileRepository.Update(profile);

        return Result.Success(uploadResult.PublicUrl);
    }
}