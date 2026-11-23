namespace OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotification;

using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public sealed record GetNotificationQuery(Guid Id) : IQuery<NotificationResponse>;
