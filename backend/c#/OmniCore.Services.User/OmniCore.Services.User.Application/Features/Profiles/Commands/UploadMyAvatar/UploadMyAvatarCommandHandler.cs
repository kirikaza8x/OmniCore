namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UploadMyAvatar;

using Microsoft.Extensions.Logging;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Storage;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.Repositories;

public sealed class UploadMyAvatarCommandHandler(
    IUserProfileRepository profiles,
    ICurrentUserService currentUser,
    IStorageService storage,
    IUnitOfWork unitOfWork,
    ILogger<UploadMyAvatarCommandHandler> logger) : ICommandHandler<UploadMyAvatarCommand, string>
{
    public async Task<Result<string>> Handle(UploadMyAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<string>(userId.Error);

        var profile = await profiles.GetByIdAsync(userId.Value, cancellationToken);
        if (profile is null) return Result.Failure<string>(UserProfileErrors.NotFound);

        // Look at the file itself, not at the content type the client claims (U5).
        var header = new byte[12];
        int read;
        await using (var probe = request.File.OpenReadStream())
        {
            read = await probe.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        }

        var contentType = ImageSignature.Detect(header.AsSpan(0, read));
        if (contentType is null)
        {
            return Result.Failure<string>(
                Error.Validation("Avatar.NotAnImage", "The file is not a JPEG, PNG or WebP image."));
        }

        var previousUrl = profile.AvatarUrl;

        await using var stream = request.File.OpenReadStream();
        var upload = await storage.UploadAsync(
            stream, $"{userId.Value.Value:N}{ImageSignature.Extension(contentType)}", contentType, "avatars", cancellationToken);

        var updated = profile.UpdateAvatar(upload.PublicUrl);
        if (updated.IsFailure) return Result.Failure<string>(updated.Error);

        // Save before deleting the old file, so a failed save never leaves the profile pointing at a deleted file.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousUrl) && previousUrl != upload.PublicUrl)
        {
            try
            {
                await storage.DeleteAsync(previousUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete the previous avatar {Url}; it is now an orphan.", previousUrl);
            }
        }

        return upload.PublicUrl;
    }
}
