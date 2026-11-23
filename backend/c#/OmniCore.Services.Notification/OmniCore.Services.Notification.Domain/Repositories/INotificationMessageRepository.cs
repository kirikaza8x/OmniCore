namespace OmniCore.Services.Notification.Domain.Repositories;

using OmniCore.Services.Notification.Domain.Entities;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.ValueObjects;
using OmniCore.Shared.Domain.Pagination;
using OmniCore.Shared.Domain.Repositories;

public interface INotificationMessageRepository : IRepository<NotificationMessage, NotificationMessageId>
{
    /// <summary>Tracked, for a redelivered request that must continue the same message.</summary>
    Task<NotificationMessage?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken = default);

    /// <summary>Newest first, untracked. <paramref name="pageSize"/> is clamped by the caller.</summary>
    Task<DomainPagedResult<NotificationMessage>> SearchAsync(
        NotificationStatus? status,
        NotificationChannel? channel,
        string? recipient,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
