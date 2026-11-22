namespace OmniCore.Services.User.IntegrationTests;

/// <summary>One API + Postgres + RabbitMQ for all test classes (RabbitMQ is bound to port 5672).</summary>
[CollectionDefinition(Name)]
public sealed class UserApiCollection : ICollectionFixture<UserApiFactory>
{
    public const string Name = "user-api";
}
