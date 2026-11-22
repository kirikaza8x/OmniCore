namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UploadMyAvatar;

using FluentValidation;

public sealed class UploadMyAvatarCommandValidator : AbstractValidator<UploadMyAvatarCommand>
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public UploadMyAvatarCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("A file is required.")
            .Must(f => f is not null && f.Length > 0).WithMessage("File cannot be empty.")
            .Must(f => f is not null && f.Length <= MaxFileSizeBytes).WithMessage("File size cannot exceed 5 MB.");
    }
}
