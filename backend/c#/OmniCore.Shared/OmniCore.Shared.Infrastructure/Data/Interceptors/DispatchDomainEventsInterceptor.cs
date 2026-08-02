namespace OmniCore.Shared.Infrastructure.Data.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OmniCore.Shared.Application.Abstractions.EventBus;
using OmniCore.Shared.Domain.DDD;

/// <summary>
/// EF Core interceptor that dispatches in-memory domain events before changes are committed to the database.
/// Any side effects (including Outbox entries added by handlers) become part of the same transaction.
/// </summary>
public sealed class DispatchDomainEventsInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            // Block sync execution or dispatch synchronously to prevent silent event loss
            DispatchDomainEventsAsync(eventData.Context, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        const int maxIterations = 10; // generous headroom over any real cascade depth
        var iteration = 0;

        while (true)
        {
            var aggregates = context.ChangeTracker
                .Entries<IAggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            if (aggregates.Count == 0)
            {
                break;
            }

            if (++iteration > maxIterations)
            {
                throw new InvalidOperationException(
                    $"Domain event dispatch exceeded {maxIterations} cascading iterations — possible infinite event loop.");
            }

            var domainEvents = aggregates
                .SelectMany(a => a.ClearDomainEvents())
                .ToList();

            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
    }
}