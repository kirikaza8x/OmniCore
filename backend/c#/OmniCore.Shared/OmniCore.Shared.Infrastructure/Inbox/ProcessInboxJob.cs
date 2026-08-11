namespace OmniCore.Shared.Infrastructure.Inbox;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniCore.Shared.Contracts.Events;
using Quartz;

[DisallowConcurrentExecution]
public sealed class ProcessInboxJob<TDbContext>(
    TDbContext dbContext,
    IServiceProvider serviceProvider,
    ILogger<ProcessInboxJob<TDbContext>> logger,
    IOptions<InboxOptions> options) : IJob
    where TDbContext : DbContext
{
    private readonly InboxOptions _options = options.Value;

    public async Task Execute(IJobExecutionContext context)
    {
        string moduleName = typeof(TDbContext).Name.Replace("DbContext", string.Empty);
        await RetryFailedMessagesAsync(moduleName, context.CancellationToken);
        await CleanupOldMessagesAsync(moduleName, context.CancellationToken);
    }

    private async Task RetryFailedMessagesAsync(string moduleName, CancellationToken cancellationToken)
    {
        var failedMessages = await dbContext.Set<InboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < _options.MaxRetryCount)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (failedMessages.Count == 0) return;

        logger.LogInformation("[{Module}] Retrying {Count} failed/pending inbox message(s).", moduleName, failedMessages.Count);

        foreach (var message in failedMessages)
        {
            message.RetryCount++;

            try
            {
                Type? eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    logger.LogWarning("[{Module}] Unknown event type '{Type}' for inbox message {Id}.", moduleName, message.Type, message.Id);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = $"Unknown event type '{message.Type}'";
                    continue;
                }

                object? integrationEvent = JsonSerializer.Deserialize(message.Content, eventType);
                if (integrationEvent == null)
                {
                    logger.LogWarning("[{Module}] Deserialization yielded null for inbox message {Id}.", moduleName, message.Id);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = "Deserialization resulted in null payload";
                    continue;
                }

                using var scope = serviceProvider.CreateScope();
                Type handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                object? handler = scope.ServiceProvider.GetService(handlerType);

                if (handler == null)
                {
                    logger.LogWarning("[{Module}] No handler registered for '{Type}'.", moduleName, message.Type);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = $"No handler registered for '{message.Type}'";
                    continue;
                }

                // FIXED: Reflected target method name is HandleAsync
                var handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync));
                if (handleMethod != null)
                {
                    await (Task)handleMethod.Invoke(handler, new[] { integrationEvent, cancellationToken })!;
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;

                logger.LogInformation("[{Module}] Successfully processed inbox message {Id}.", moduleName, message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{Module}] Failed inbox message execution {Id} (Attempt {Attempt}/{Max}).", 
                    moduleName, message.Id, message.RetryCount, _options.MaxRetryCount);

                message.Error = ex.Message;

                if (message.RetryCount >= _options.MaxRetryCount)
                {
                    logger.LogError("[{Module}] Inbox message {Id} reached max retries ({Max}). Marked as completed with error.", 
                        moduleName, message.Id, _options.MaxRetryCount);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupOldMessagesAsync(string moduleName, CancellationToken cancellationToken)
    {
        DateTime cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        int deletedCount = await dbContext.Set<InboxMessage>()
            .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("[{Module}] Cleaned up {Count} processed inbox messages older than {Days} days.", moduleName, deletedCount, _options.RetentionDays);
        }
    }
}