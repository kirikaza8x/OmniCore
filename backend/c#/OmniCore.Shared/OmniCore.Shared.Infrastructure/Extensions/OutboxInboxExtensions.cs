namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Infrastructure.Inbox;
using OmniCore.Shared.Infrastructure.Outbox;

/// <summary>
/// Extension methods for registering Outbox and Inbox background workers.
/// </summary>
public static class OutboxInboxExtensions
{
    /// <summary>
    /// Registers the Outbox and Inbox Quartz options configurators for the specified DbContext.
    /// </summary>
    /// <typeparam name="TDbContext">The targeted DbContext type.</typeparam>
    public static IServiceCollection AddOutboxAndInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddConfig<OutboxOptions>();
        services.AddConfig<InboxOptions>();

        services.ConfigureOptions<ConfigureProcessOutboxJob<TDbContext>>();
        services.ConfigureOptions<ConfigureProcessInboxJob<TDbContext>>();

        return services;
    }
}