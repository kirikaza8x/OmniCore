namespace OmniCore.Services.User.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Application.Features.TenantProfiles.GetMyTenantProfile;
using OmniCore.Services.User.Application.Features.TenantProfiles.UpsertMyTenantProfile;
using OmniCore.Shared.Api.Extensions;

/// <summary>
/// The caller's profile inside the tenant of the token (ADR 0001). The user id and the tenant both come from the
/// token, never from the request.
/// </summary>
public sealed class TenantProfileEndpoints : ICarterModule
{
    private static readonly UserRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.GroupPrefix).WithTags(Routes.Tag);

        group.MapGet(Routes.MyTenantProfile, async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyTenantProfileQuery(), ct)).ToOk("Tenant profile retrieved successfully"))
            .WithName("GetMyTenantProfile")
            .RequireAuthorization()
            .Produces<ApiResult<TenantProfileResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(Routes.MyTenantProfile, async ([FromBody] UpsertMyTenantProfileRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpsertMyTenantProfileCommand(body.GamerTag, body.CustomAvatarUrl), ct))
                    .ToOk("Tenant profile saved successfully"))
            .WithName("UpsertMyTenantProfile")
            .RequireAuthorization()
            .Produces<ApiResult<TenantProfileResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
