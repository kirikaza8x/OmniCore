namespace OmniCore.Shared.Infrastructure.Data.Interceptors;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Infrastructure.Outbox;

public sealed class ConvertDomainEventsToOutboxMessagesInterceptor(
    ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> logger) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ConvertDomainEventsToOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ConvertDomainEventsToOutboxMessages(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void ConvertDomainEventsToOutboxMessages(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0) return;

        var domainEvents = aggregates
            .SelectMany(a => a.ClearDomainEvents())
            .ToList();

        var outboxMessages = domainEvents
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.EventId != Guid.Empty ? domainEvent.EventId : Guid.NewGuid(),
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                OccurredOnUtc = DateTime.UtcNow
            })
            .ToList();

        logger.LogInformation(
            "Captured {Count} domain event(s) into Outbox from {AggregateCount} aggregate(s).",
            outboxMessages.Count,
            aggregates.Count);

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}