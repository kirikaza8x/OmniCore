namespace OmniCore.Shared.Infrastructure.Inbox;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using OmniCore.Shared.Application.Abstractions.EventBus;

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
        var moduleName = typeof(TDbContext).Name.Replace("DbContext", "");
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

        if (failedMessages.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "[{Module}] Retrying {Count} failed/pending inbox messages",
            moduleName,
            failedMessages.Count);

        foreach (var message in failedMessages)
        {
            message.RetryCount++;

            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    logger.LogWarning("[{Module}] Unknown event type: {Type}", moduleName, message.Type);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = $"Unknown type '{message.Type}'";
                    continue;
                }

                var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType);
                if (integrationEvent == null)
                {
                    logger.LogWarning("[{Module}] Deserialization yielded null for message {Id}", moduleName, message.Id);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = "Deserialization resulted in null payload";
                    continue;
                }

                // Scope handler resolution to prevent DI lifetime leaks
                using var scope = serviceProvider.CreateScope();
                var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                var handler = scope.ServiceProvider.GetService(handlerType);

                if (handler == null)
                {
                    logger.LogWarning("[{Module}] No handler registered for {Type}", moduleName, message.Type);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    message.Error = $"No handler registered for '{message.Type}'";
                    continue;
                }

                var handleMethod = handlerType.GetMethod("Handle");
                if (handleMethod != null)
                {
                    await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;

                logger.LogInformation("[{Module}] Successfully processed message {Id}", moduleName, message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{Module}] Retry failed for message {Id} (Attempt {Attempt}/{Max})", 
                    moduleName, message.Id, message.RetryCount, _options.MaxRetryCount);

                message.Error = ex.Message;

                if (message.RetryCount >= _options.MaxRetryCount)
                {
                    logger.LogError("[{Module}] Message {Id} reached max retry limit ({Max}). Marking as completed with error.", 
                        moduleName, message.Id, _options.MaxRetryCount);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupOldMessagesAsync(string moduleName, CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        var deletedCount = await dbContext.Set<InboxMessage>()
            .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation(
                "[{Module}] Cleaned up {Count} old processed inbox messages",
                moduleName,
                deletedCount);
        }
    }
}