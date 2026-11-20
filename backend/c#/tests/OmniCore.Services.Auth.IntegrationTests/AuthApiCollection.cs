namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>One API + Postgres + RabbitMQ for every test class that uses it (RabbitMQ is bound to the fixed port 5672,
/// so two factories at once cannot start).</summary>
[CollectionDefinition(Name)]
public sealed class AuthApiCollection : ICollectionFixture<AuthApiFactory>
{
    public const string Name = "auth-api";
}
