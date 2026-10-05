namespace OmniCore.Shared.Infrastructure.Extensions;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniCore.Shared.Infrastructure.Configs.MessageBroker;

internal static class OutboxExtensions
{
    public static void ConfigureTransactionalOutbox<TDbContext>(
        this IBusRegistrationConfigurator busConfigurator,
        MessageBrokerConfig brokerConfig,
        Action<IEntityFrameworkOutboxConfigurator>? configureOutbox)
        where TDbContext : DbContext
    {
        if (!brokerConfig.EnableOutbox) return;

        busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outbox =>
        {
            outbox.UseBusOutbox();

            if (configureOutbox is not null)
            {
                configureOutbox(outbox);
            }
            else
            {
                ApplyOutboxDbProvider(outbox, brokerConfig.OutboxDbProvider);
            }
        });
    }

    private static void ApplyOutboxDbProvider(IEntityFrameworkOutboxConfigurator outbox, string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("Transactional Outbox is enabled, but MessageBroker:OutboxDbProvider configuration is missing.");
        }

        switch (providerName.Trim().ToLowerInvariant())
        {
            case "postgres":
            case "postgresql":
            case "npgsql":
                outbox.UsePostgres();
                break;
            case "sqlserver":
            case "mssql":
                outbox.UseSqlServer();
                break;
            case "mysql":
            case "mariadb":
                outbox.UseMySql();
                break;
            case "sqlite":
                outbox.UseSqlite();
                break;
            default:
                throw new NotSupportedException($"Unsupported Outbox database provider: '{providerName}'.");
        }
    }
}