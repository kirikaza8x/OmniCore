namespace OmniCore.Services.Notification.IntegrationTests;

[Collection(NotificationApiCollection.Name)]
public sealed class HealthTests(NotificationApiFactory factory)
{
    [Fact]
    public async Task Service_starts_applies_migrations_and_reports_healthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode, $"/health answered {(int)response.StatusCode}");
    }
}
