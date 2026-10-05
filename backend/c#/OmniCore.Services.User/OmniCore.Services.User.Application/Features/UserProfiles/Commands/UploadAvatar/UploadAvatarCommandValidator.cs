// Features/UserProfiles/Commands/UploadAvatar/UploadAvatarCommandValidator.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.UploadAvatar;

using FluentValidation;

public sealed class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(f => f != null && f.Length > 0).WithMessage("File cannot be empty.")
            .Must(f => f != null && f.Length <= MaxFileSizeBytes).WithMessage("File size cannot exceed 5 MB.")
            .Must(f => f != null && AllowedContentTypes.Contains(f.ContentType.ToLowerInvariant()))
            .WithMessage("Only JPEG, PNG, and WebP image formats are allowed.");
    }
}