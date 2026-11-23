namespace OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotifications;

using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.Pagination;

public sealed class GetNotificationsQueryHandler(INotificationMessageRepository messages)
    : IQueryHandler<GetNotificationsQuery, DomainPagedResult<NotificationResponse>>
{
    public async Task<Result<DomainPagedResult<NotificationResponse>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        // Clamped here as well as in the repository: a client cannot ask for an unbounded page.
        var page = Math.Max(request.PageNumber, 1);
        var size = Math.Clamp(request.PageSize, 1, 100);

        NotificationStatus? status = request.Status is null ? null : Enum.Parse<NotificationStatus>(request.Status, true);
        NotificationChannel? channel = request.Channel is null ? null : Enum.Parse<NotificationChannel>(request.Channel, true);

        var result = await messages.SearchAsync(status, channel, request.Recipient?.Trim(), page, size, cancellationToken);
        return result.Map(m => m.ToResponse());
    }
}
