using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>
/// Starts Postgres and RabbitMQ in Docker and points the Auth API at them through environment variables
/// (the service reads all its settings from there, appsettings files are not in the repo).
/// Written without a .NET SDK or Docker: not compiled or run yet.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>The key Auth signs with in the tests; tests also use it to check tokens and the JWKS.</summary>
    public static readonly System.Security.Cryptography.RSA SigningKey = System.Security.Cryptography.RSA.Create(2048);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder().WithImage("rabbitmq:3.13-alpine")
        .WithPortBinding(5672, 5672)   // MessageBrokerConfig has no Port setting, so the broker must be on 5672
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
            ["MessageBroker__Username"] = "guest",
            ["MessageBroker__Password"] = "guest",
            ["MessageBroker__OutboxDbProvider"] = "Postgres",
            ["MessageBroker__EnableOutbox"] = "true",
            // The API runs in the post-cut-over state (ADR 0003 step 4): RS256 only, no legacy HS256 secret.
            ["Jwt__PrivateKeyPem"] = SigningKey.ExportPkcs8PrivateKeyPem(),
            ["Jwt__Secret"] = "",
            ["Jwt__Issuer"] = "omnicore-tests",
            ["Jwt__Audience"] = "omnicore-tests",
            ["GoogleAuth__ServerClientId"] = "CHANGE_ME",
            ["GoogleAuth__ClientId"] = "CHANGE_ME",
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
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbit.DisposeAsync();
        await base.DisposeAsync();
    }
}
