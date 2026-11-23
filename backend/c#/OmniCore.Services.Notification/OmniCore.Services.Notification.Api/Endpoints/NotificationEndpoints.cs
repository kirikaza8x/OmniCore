namespace OmniCore.Services.Notification.Api.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;
using OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotification;
using OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotifications;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Shared.Api.Extensions;
using OmniCore.Shared.Domain.Pagination;

/// <summary>
/// Administration of notifications: the delivery log and a test send. Admin and SuperAdmin only. Notifications are
/// normally created by integration events, not by this API.
/// </summary>
public sealed class NotificationEndpoints : ICarterModule
{
    private static readonly NotificationRoutes Routes = new();

    public sealed record SendTestNotificationRequest(string Channel, string Recipient);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.GroupPrefix).WithTags(Routes.Tag);

        group.MapGet(Routes.GetAll, async (
                [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? status,
                [FromQuery] string? channel, [FromQuery] string? recipient,
                ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetNotificationsQuery(page ?? 1, pageSize ?? 20, status, channel, recipient), ct))
                    .ToOk("Notifications retrieved successfully"))
            .WithName("GetNotifications")
            .RequireAuthorization()
            .RequireRoles("Admin", "SuperAdmin")
            .Produces<ApiResult<DomainPagedResult<NotificationResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(Routes.GetById, async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetNotificationQuery(id), ct)).ToOk("Notification retrieved successfully"))
            .WithName("GetNotification")
            .RequireAuthorization()
            .RequireRoles("Admin", "SuperAdmin")
            .Produces<ApiResult<NotificationResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(Routes.Test, async ([FromBody] SendTestNotificationRequest body, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse<NotificationChannel>(body.Channel, ignoreCase: true, out var channel) || !Enum.IsDefined(channel))
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown channel", detail: "Channel must be Email or Webhook.");
                }

                var model = new Dictionary<string, string> { ["appName"] = "OmniCore", ["sentBy"] = "an administrator" };
                // No deduplication key: every call is a new send.
                return (await sender.Send(new SendNotificationCommand(channel, body.Recipient, "test", model, null), ct))
                    .ToOk("Test notification processed");
            })
            .WithName("SendTestNotification")
            .RequireAuthorization()
            .RequireRoles("Admin", "SuperAdmin")
            .Produces<ApiResult<NotificationResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
