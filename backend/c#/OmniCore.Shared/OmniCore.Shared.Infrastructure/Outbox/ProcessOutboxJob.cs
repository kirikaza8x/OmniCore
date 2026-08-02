namespace OmniCore.Shared.Infrastructure.Outbox;

using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxJob<TDbContext>(
    TDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<ProcessOutboxJob<TDbContext>> logger,
    IOptions<OutboxOptions> options) : IJob
    where TDbContext : DbContext
{
    private readonly OutboxOptions _options = options.Value;

    public async Task Execute(IJobExecutionContext context)
    {
        string moduleName = typeof(TDbContext).Name.Replace("DbContext", string.Empty);

        List<OutboxMessage> messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0) return;

        logger.LogDebug("[{Module}] Found {Count} outbox message(s) to process.", moduleName, messages.Count);

        foreach (OutboxMessage message in messages)
        {
            try
            {
                Type? eventType = Type.GetType(message.Type);
                if (eventType is null)
                {
                    logger.LogError("[{Module}] Outbox message {Id} has unknown type '{Type}'.", moduleName, message.Id, message.Type);
                    message.Error = $"Unknown assembly type: {message.Type}";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                object? domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
                if (domainEvent is null)
                {
                    logger.LogError("[{Module}] Outbox message {Id} deserialization returned null.", moduleName, message.Id);
                    message.Error = "Deserialization failed.";
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    continue;
                }

                // Explicit runtime type publish for correct MassTransit exchange targeting
                await publishEndpoint.Publish(domainEvent, eventType, context.CancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;

                logger.LogDebug("[{Module}] Processed outbox message {Id} ({Type}).", moduleName, message.Id, eventType.Name);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= _options.MaxRetryCount)
                {
                    message.ProcessedOnUtc = DateTime.UtcNow; // Dead-letter after max retries
                    logger.LogError(ex, "[{Module}] Outbox message {Id} reached max retry limit ({MaxRetries}) and was marked as failed.", moduleName, message.Id, _options.MaxRetryCount);
                }
                else
                {
                    logger.LogWarning(ex, "[{Module}] Outbox message {Id} failed attempt {RetryCount}/{MaxRetries}.", moduleName, message.Id, message.RetryCount, _options.MaxRetryCount);
                }
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}