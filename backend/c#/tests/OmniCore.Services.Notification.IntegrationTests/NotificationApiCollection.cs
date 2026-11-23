namespace OmniCore.Services.Notification.IntegrationTests;

/// <summary>One API + Postgres + RabbitMQ for all test classes (RabbitMQ is bound to port 5672).</summary>
[CollectionDefinition(Name)]
public sealed class NotificationApiCollection : ICollectionFixture<NotificationApiFactory>
{
    public const string Name = "notification-api";
}
