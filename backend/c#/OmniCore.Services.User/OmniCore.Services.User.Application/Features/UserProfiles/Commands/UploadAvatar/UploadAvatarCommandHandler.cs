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

        var previousAvatarUrl = profile.AvatarUrl;

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

        // Remove the previous file only after the new one is uploaded and attached; deleting it first left the
        // profile pointing at a missing file whenever the upload failed.
        if (!string.IsNullOrWhiteSpace(previousAvatarUrl) && previousAvatarUrl != uploadResult.PublicUrl)
        {
            try
            {
                await storageService.DeleteAsync(previousAvatarUrl, cancellationToken);
            }
            catch
            {
                // Best-effort cleanup for old image
            }
        }

        return Result.Success(uploadResult.PublicUrl);
    }
}