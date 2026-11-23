using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace OmniCore.Services.Notification.IntegrationTests;

/// <summary>Postgres + RabbitMQ in Docker; the Notification API reads all its settings from environment variables.
/// Tokens are the legacy HS256 kind (<c>Jwt__Secret</c>) so tests can mint them without a running Auth service.</summary>
public sealed class NotificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Secret = "integration-test-secret-0123456789-abcdef";
    public const string Issuer = "omnicore-tests";
    public const string Audience = "omnicore-tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder().WithImage("rabbitmq:3.13-alpine")
        .WithPortBinding(5672, 5672)   // MessageBrokerConfig has no Port setting
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());

        var settings = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["Database__ConnectionString"] = _postgres.GetConnectionString(),
            ["MessageBroker__Provider"] = "RabbitMQ",
            ["MessageBroker__Host"] = _rabbit.Hostname,
            ["MessageBroker__Username"] = "rabbitmq",
            ["MessageBroker__Password"] = "rabbitmq",
            ["MessageBroker__OutboxDbProvider"] = "Postgres",
            ["MessageBroker__EnableOutbox"] = "true",
            ["Jwt__Secret"] = Secret,
            ["Jwt__Issuer"] = Issuer,
            ["Jwt__Audience"] = Audience,
            ["Cors__AllowAnyOrigin"] = "false",
            ["RateLimiting__GlobalPolicy"] = "fixed",
            ["RateLimiting__Policies__fixed__PermitLimit"] = "1000",
            ["RateLimiting__Policies__fixed__WindowSeconds"] = "60",
            ["RateLimiting__Policies__fixed__QueueLimit"] = "10",
            ["Redis__Host"] = "localhost",
            ["Redis__Port"] = "6379",
            ["Storage__Endpoint"] = "http://localhost:9000",
            ["Storage__AccessKey"] = "CHANGE_ME",
            ["Storage__SecretKey"] = "CHANGE_ME",
            ["Storage__BucketName"] = "tests",
        };
        foreach (var (key, value) in settings)
            Environment.SetEnvironmentVariable(key, value);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbit.DisposeAsync();
        await base.DisposeAsync();
    }
}
