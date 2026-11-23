namespace OmniCore.Services.Notification.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OmniCore.Services.Notification.Domain.Entities;
using OmniCore.Services.Notification.Domain.Enums;
using OmniCore.Services.Notification.Domain.Repositories;
using OmniCore.Services.Notification.Domain.ValueObjects;
using OmniCore.Services.Notification.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Domain.Pagination;
using OmniCore.Shared.Infrastructure.Data.Repositories;

public class NotificationMessageRepository(NotificationDbContext dbContext)
    : RepositoryBase<NotificationMessage, NotificationMessageId>(dbContext), INotificationMessageRepository
{
    public Task<NotificationMessage?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(m => m.DeduplicationKey == deduplicationKey, cancellationToken);

    public async Task<DomainPagedResult<NotificationMessage>> SearchAsync(
        NotificationStatus? status,
        NotificationChannel? channel,
        string? recipient,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = DbSet.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(m => m.Status == status);
        if (channel is not null) query = query.Where(m => m.Channel == channel);
        if (!string.IsNullOrWhiteSpace(recipient)) query = query.Where(m => m.Recipient == recipient);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return DomainPagedResult<NotificationMessage>.Create(items, pageNumber, pageSize, total);
    }
}
