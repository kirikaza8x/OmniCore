namespace OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotification;

using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Services.Notification.Domain.Errors;
using OmniCore.Services.Notification.Domain.Repositories;
using OmniCore.Services.Notification.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetNotificationQueryHandler(INotificationMessageRepository messages)
    : IQueryHandler<GetNotificationQuery, NotificationResponse>
{
    public async Task<Result<NotificationResponse>> Handle(GetNotificationQuery request, CancellationToken cancellationToken)
    {
        var message = await messages.GetByIdAsync(NotificationMessageId.From(request.Id), cancellationToken);
        return message is null
            ? Result.Failure<NotificationResponse>(NotificationErrors.NotFound)
            : message.ToResponse();
    }
}
