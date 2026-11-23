using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Services.Auth.Contracts.Events;
using OmniCore.Services.Notification.Contracts.Events;

namespace OmniCore.Services.Notification.IntegrationTests;

[Collection(NotificationApiCollection.Name)]
public class NotificationApiTests(NotificationApiFactory factory)
{
    private const string Base = "/api/v1/notifications";

    private static string Token(params string[] roles)
    {
        var id = Guid.NewGuid().ToString();
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, id), new(ClaimTypes.NameIdentifier, id) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(NotificationApiFactory.Secret));
        var jwt = new JwtSecurityToken(NotificationApiFactory.Issuer, NotificationApiFactory.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private HttpClient ClientFor(params string[] roles)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(roles));
        return client;
    }

    private static JsonElement? Find(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var p in element.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
                var nested = Find(p.Value, name);
                if (nested is not null) return nested;
            }
        return null;
    }

    private static async Task<JsonElement> Body(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<JsonElement>())!;

    private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.com";

    private IEnumerable<string> SentTo(string address) =>
        factory.Email.Sent.Where(m => m.To.Contains(address)).Select(m => m.Subject);

    private static async Task Eventually(Func<bool> condition, string because)
    {
        for (var i = 0; i < 60; i++)
        {
            if (condition()) return;
            await Task.Delay(500);
        }
        Assert.Fail($"Timed out waiting for: {because}");
    }

    private async Task<JsonElement> LogFor(string recipient)
    {
        var response = await ClientFor("Admin").GetAsync($"{Base}?recipient={Uri.EscapeDataString(recipient)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Body(response);
    }

    [Fact]
    public async Task Endpoints_need_a_token_and_the_admin_role()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync(Base)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor().GetAsync(Base)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientFor("User").GetAsync($"{Base}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await ClientFor("User").PostAsJsonAsync($"{Base}/test", new { channel = "Email", recipient = "a@example.com" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ClientFor("Admin").GetAsync(Base)).StatusCode);
    }

    [Fact]
    public async Task An_unknown_notification_is_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await ClientFor("Admin").GetAsync($"{Base}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task A_bad_list_filter_is_a_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await ClientFor("Admin").GetAsync($"{Base}?status=Exploded")).StatusCode);
    }

    [Fact]
    public async Task The_test_endpoint_sends_an_email_and_logs_it()
    {
        var to = UniqueEmail();

        var response = await ClientFor("Admin").PostAsJsonAsync($"{Base}/test", new { channel = "Email", recipient = to });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Sent", Find(await Body(response), "status")!.Value.GetString());
        Assert.Contains("OmniCore test notification", SentTo(to));

        var log = await LogFor(to);
        Assert.Equal(1, Find(log, "totalCount")!.Value.GetInt32());
    }

    [Fact]
    public async Task A_webhook_to_an_endpoint_that_is_not_configured_fails_permanently()
    {
        var response = await ClientFor("Admin").PostAsJsonAsync($"{Base}/test", new { channel = "Webhook", recipient = "nohook" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Body(response);
        Assert.Equal("Failed", Find(body, "status")!.Value.GetString());
        Assert.Contains("nohook", Find(body, "lastError")!.Value.GetString());
    }

    [Fact]
    public async Task An_invalid_recipient_is_a_400_and_nothing_is_sent()
    {
        var response = await ClientFor("Admin").PostAsJsonAsync($"{Base}/test", new { channel = "Email", recipient = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_registered_account_gets_one_welcome_mail_even_if_the_event_arrives_twice()
    {
        var accountId = Guid.NewGuid();
        var email = UniqueEmail();
        using (var scope = factory.Services.CreateScope())
        {
            // IBus publishes straight to the broker (the outbox belongs to IPublishEndpoint inside a unit of work).
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();
            await bus.Publish(new AccountRegisteredIntegrationEvent(accountId, "ann", email, "Ann", "Lee"));
            await bus.Publish(new AccountRegisteredIntegrationEvent(accountId, "ann", email, "Ann", "Lee"));
        }

        await Eventually(() => SentTo(email).Any(), "the welcome mail");
        await Task.Delay(2000); // time for the duplicate to be processed
        Assert.Single(SentTo(email));
        Assert.Contains("Welcome to OmniCore", SentTo(email));
    }

    [Fact]
    public async Task A_temporary_mail_outage_is_retried_until_it_works()
    {
        var to = UniqueEmail();
        factory.Email.FailNext(1);

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IBus>().Publish(new NotificationRequestedIntegrationEvent(
                "Email", to, "welcome", new Dictionary<string, string> { ["name"] = "Ann", ["appName"] = "OmniCore" }, $"retry:{to}"));
        }

        await Eventually(() => SentTo(to).Any(), "the mail after the retry");
        var log = await LogFor(to);
        var item = Find(log, "items")!.Value[0];
        Assert.Equal("Sent", item.GetProperty("status").GetString());
        // Not asserted: attempts == 2. When the broker's inbox transaction rolls back after the handler throws, the failed
        // attempt's row can roll back with it (ADR 0006), so only the committed attempt is guaranteed to be in the log.
        Assert.True(item.GetProperty("attempts").GetInt32() >= 1);
    }
}
