namespace OmniCore.Shared.Infrastructure.Data.Interceptors;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Infrastructure.Outbox;

public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ConvertDomainEventsToOutboxMessagesInterceptor(
        IDateTimeProvider dateTimeProvider,
        ILogger<ConvertDomainEventsToOutboxMessagesInterceptor> logger)
    {
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ConvertDomainEventsToOutboxMessages(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

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

        var utcNow = _dateTimeProvider.UtcNow;

        var outboxMessages = domainEvents
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.EventId != Guid.Empty ? domainEvent.EventId : Guid.NewGuid(),
                // FIXED: Use FullName instead of AssemblyQualifiedName for flexible event type resolution
                Type = domainEvent.GetType().FullName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                OccurredOnUtc = utcNow
            })
            .ToList();

        _logger.LogInformation(
            "Captured {Count} domain event(s) into Outbox from {AggregateCount} aggregate(s).",
            outboxMessages.Count,
            aggregates.Count);

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}