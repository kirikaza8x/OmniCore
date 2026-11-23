namespace OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotifications;

using OmniCore.Services.Notification.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Pagination;

/// <summary>The delivery log, newest first. <c>Status</c>/<c>Channel</c> are enum names; <c>Recipient</c> is an exact match.</summary>
public sealed record GetNotificationsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Status = null,
    string? Channel = null,
    string? Recipient = null) : IQuery<DomainPagedResult<NotificationResponse>>;
