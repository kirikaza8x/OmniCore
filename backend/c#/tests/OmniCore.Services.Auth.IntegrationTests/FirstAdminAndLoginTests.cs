using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniCore.Services.Auth.IntegrationTests;

[Collection(AuthApiCollection.Name)]
public class FirstAdminAndLoginTests(AuthApiFactory factory)
{
    private const string Base = "/api/v1/auth";

    private static string? Find(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                return p.Value.GetString();
            var nested = Find(p.Value, name);
            if (nested is not null) return nested;
        }
        return null;
    }

    [Fact]
    public async Task The_seeded_first_admin_can_log_in_and_reach_admin_endpoints_while_normal_users_cannot()
    {
        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync($"{Base}/login",
            new { identifier = AuthApiFactory.AdminUsername, password = AuthApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var adminToken = Find((await login.Content.ReadFromJsonAsync<JsonElement>())!, "accessToken");

        var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Base}/roles")).StatusCode);

        var user = $"plain_{Guid.NewGuid():N}"[..20];
        const string password = "Sup3r!Secret-test";
        var register = await client.PostAsJsonAsync($"{Base}/register", new { username = user, email = $"{user}@example.com", password });
        var userToken = Find((await register.Content.ReadFromJsonAsync<JsonElement>())!, "accessToken");
        var plain = factory.CreateClient();
        plain.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await plain.GetAsync($"{Base}/roles")).StatusCode);
    }

    [Fact]
    public async Task Every_failed_login_looks_the_same()
    {
        var client = factory.CreateClient();
        var user = $"known_{Guid.NewGuid():N}"[..20];
        const string password = "Sup3r!Secret-test";
        await client.PostAsJsonAsync($"{Base}/register", new { username = user, email = $"{user}@example.com", password });

        var unknown = await client.PostAsJsonAsync($"{Base}/login", new { identifier = "nobody_here_x", password });
        var wrong = await client.PostAsJsonAsync($"{Base}/login", new { identifier = user, password = "Wrong-password-1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await wrong.Content.ReadAsStringAsync());
    }
}
