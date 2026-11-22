namespace OmniCore.Services.User.Application.Features.Profiles.Commands.UploadMyAvatar;

using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Storage;

/// <summary>Replaces the caller's avatar. Returns the public URL of the new image.</summary>
public record UploadMyAvatarCommand(IFileUpload File) : ICommand<string>;
