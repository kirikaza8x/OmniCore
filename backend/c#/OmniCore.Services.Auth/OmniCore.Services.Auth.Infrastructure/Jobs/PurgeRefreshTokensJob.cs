namespace OmniCore.Services.Auth.Infrastructure.Jobs;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniCore.Services.Auth.Infrastructure.Configs;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using Quartz;

/// <summary>
/// Deletes refresh-token rows that expired more than <see cref="RefreshTokenCleanupConfig.RetentionDays"/> days ago.
/// Active tokens are never touched. One bulk DELETE, so it is safe to run on every replica (A10).
/// </summary>
[DisallowConcurrentExecution]
public sealed class PurgeRefreshTokensJob(
    AuthDbContext db,
    IOptions<RefreshTokenCleanupConfig> options,
    ILogger<PurgeRefreshTokensJob> logger) : IJob
{
    public static readonly JobKey Key = new("purge-refresh-tokens", "auth");

    public async Task Execute(IJobExecutionContext context)
    {
        var deleted = await PurgeAsync(db, options.Value.RetentionDays, DateTime.UtcNow, context.CancellationToken);
        logger.LogInformation("Purged {Count} expired refresh tokens older than {Days} days.", deleted, options.Value.RetentionDays);
    }

    public static Task<int> PurgeAsync(AuthDbContext db, int retentionDays, DateTime nowUtc, CancellationToken ct)
    {
        var cutoff = nowUtc.AddDays(-retentionDays);
        return db.RefreshTokens.Where(t => t.ExpiresAtUtc < cutoff).ExecuteDeleteAsync(ct);
    }
}
