namespace OmniCore.Services.Notification.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OmniCore.Services.Notification.Application.Abstractions;
using OmniCore.Services.Notification.Infrastructure.Channels;
using OmniCore.Services.Notification.Infrastructure.Configs;
using OmniCore.Services.Notification.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Application.Abstractions.Notifications;
using OmniCore.Shared.Domain.Repositories;
using OmniCore.Shared.Infrastructure;
using OmniCore.Shared.Infrastructure.Configs.Database;
using OmniCore.Shared.Infrastructure.Data;
using OmniCore.Shared.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSharedInfrastructure(configuration, [InfrastructureAssemblyReference.Assembly]);

        services.AddDbContext<NotificationDbContext>((sp, options) =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseConfig>>().Value;

            options.UseNpgsql(dbConfig.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.SchemaName);

                if (dbConfig.MaxRetryCount > 0)
                    npgsqlOptions.EnableRetryOnFailure(dbConfig.MaxRetryCount);

                if (dbConfig.CommandTimeout > 0)
                    npgsqlOptions.CommandTimeout(dbConfig.CommandTimeout);
            })
            .UseSnakeCaseNamingConvention();

            if (dbConfig.EnableDetailedErrors) options.EnableDetailedErrors();
            if (dbConfig.EnableSensitiveDataLogging) options.EnableSensitiveDataLogging();

            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWorkBase<NotificationDbContext>>();

        // Integration-event consumers may live in Infrastructure or Application, so both assemblies are scanned.
        services.AddMassTransitWithBroker<NotificationDbContext>(
            configuration,
            InfrastructureAssemblyReference.Assembly,
            Application.ApplicationAssemblyReference.Assembly);

        services.Scan(scan => scan
            .FromAssemblies(InfrastructureAssemblyReference.Assembly)
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddDataSeeders(InfrastructureAssemblyReference.Assembly);

        // Channels: one INotificationChannel per way of delivering (ADR 0006). To add one, implement the interface and
        // register it here; nothing else changes.
        services.AddConfig<SmtpConfig>();
        services.AddConfig<WebhooksConfig>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient(WebhookChannel.HttpClientName);
        services.AddScoped<INotificationChannel, EmailChannel>();
        services.AddScoped<INotificationChannel, WebhookChannel>();

        return services;
    }

    /// <summary>Applies EF migrations (and seeders) at startup.</summary>
    public static async Task<IApplicationBuilder> UseNotificationInfrastructureAsync(
        this IApplicationBuilder app,
        bool seedData = true)
    {
        // Production must not point at the development mail catcher: mail would "send" fine and reach nobody.
        var provider = app.ApplicationServices;
        if (provider.GetRequiredService<IHostEnvironment>().IsProduction())
        {
            var host = provider.GetRequiredService<IOptions<SmtpConfig>>().Value.Host.Trim().ToLowerInvariant();
            if (host is "mailpit" or "localhost" or "127.0.0.1" or "::1")
            {
                throw new InvalidOperationException(
                    $"Smtp:Host is '{host}', a development mail catcher. Set SMTP_HOST to a real SMTP relay in production.");
            }
        }

        await app.ApplyMigrationsAsync<NotificationDbContext>(seedData);
        return app;
    }
}
