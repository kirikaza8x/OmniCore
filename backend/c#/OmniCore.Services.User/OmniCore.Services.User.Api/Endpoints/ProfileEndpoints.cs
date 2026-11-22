namespace OmniCore.Services.User.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMyProfile;
using OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMySettings;
using OmniCore.Services.User.Application.Features.Profiles.Commands.UploadMyAvatar;
using OmniCore.Services.User.Application.Features.Profiles.Queries.GetMyProfile;
using OmniCore.Services.User.Application.Features.Profiles.Queries.GetProfileDetails;
using OmniCore.Services.User.Application.Features.Profiles.Queries.GetPublicProfile;
using OmniCore.Shared.Api.Extensions;
using OmniCore.Shared.Api.File;

/// <summary>
/// Profile, settings and avatar. Every "me" endpoint acts on the signed-in user: the id comes from the token and is
/// never part of a request (ADR 0004). Profiles are created by the broker, not here.
/// </summary>
public sealed class ProfileEndpoints : ICarterModule
{
    private static readonly UserRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.GroupPrefix).WithTags(Routes.Tag);

        group.MapGet(Routes.Me, async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyProfileQuery(), ct)).ToOk("Profile retrieved successfully"))
            .WithName("GetMyProfile")
            .RequireAuthorization()
            .Produces<ApiResult<UserProfileResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(Routes.Me, async ([FromBody] UpdateMyProfileRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateMyProfileCommand(body.FirstName, body.LastName, body.Bio, body.PhoneNumber), ct))
                    .ToOk("Profile updated successfully"))
            .WithName("UpdateMyProfile")
            .RequireAuthorization()
            .Produces<ApiResult<UserProfileResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(Routes.MySettings, async ([FromBody] UpdateMySettingsRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(
                    new UpdateMySettingsCommand(body.Theme, body.Language, body.EmailNotificationsEnabled, body.PushNotificationsEnabled), ct))
                    .ToOk("Settings updated successfully"))
            .WithName("UpdateMySettings")
            .RequireAuthorization()
            .Produces<ApiResult<UserSettingsResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(Routes.MyAvatar, async (IFormFile file, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UploadMyAvatarCommand(new FormFileUpload(file)), ct)).ToOk("Avatar updated successfully"))
            .WithName("UploadMyAvatar")
            .RequireAuthorization()
            .DisableAntiforgery() // bearer-token API, no cookies: there is nothing to forge
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ApiResult<string>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Another user's public data (display name, avatar, bio). Any signed-in user.
        group.MapGet(Routes.PublicProfile, async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPublicProfileQuery(id), ct)).ToOk("Profile retrieved successfully"))
            .WithName("GetPublicProfile")
            .RequireAuthorization()
            .Produces<ApiResult<PublicProfileResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Everything we store about a user (e-mail, phone, addresses, settings). Admins only.
        group.MapGet(Routes.ProfileDetails, async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetProfileDetailsQuery(id), ct)).ToOk("Profile retrieved successfully"))
            .WithName("GetProfileDetails")
            .RequireRoles("Admin", "SuperAdmin")
            .Produces<ApiResult<UserProfileResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
