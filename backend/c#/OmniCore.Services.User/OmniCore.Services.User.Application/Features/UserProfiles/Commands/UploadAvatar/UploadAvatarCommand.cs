// Features/UserProfiles/Commands/UploadAvatar/UploadAvatarCommand.cs
namespace OmniCore.Services.User.Application.Features.UserProfiles.Commands.UploadAvatar;

using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Storage;

public record UploadAvatarCommand(
    Guid UserId,
    IFileUpload File) : ICommand<string>;