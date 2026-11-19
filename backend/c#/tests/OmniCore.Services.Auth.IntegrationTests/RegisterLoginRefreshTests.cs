using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniCore.Services.Auth.IntegrationTests;

// Written without a .NET SDK or Docker: not compiled or run yet.
public class RegisterLoginRefreshTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private const string Base = "/api/v1/auth";

    // The API wraps results in ApiResult<T>; search for the property instead of hard-coding the wrapper shape.
    private static string? Find(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
                var nested = Find(property.Value, name);
                if (nested is not null) return nested;
            }
        }
        return null;
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    [Fact]
    public async Task Register_Login_Refresh_RotatesTheTokenAndRejectsReuse()
    {
        var client = factory.CreateClient();
        var user = $"user_{Guid.NewGuid():N}"[..20];
        var email = $"{user}@example.com";
        const string password = "Sup3r!Secret-test";

        var register = await client.PostAsJsonAsync($"{Base}/register", new { username = user, email, password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync($"{Base}/login", new { identifier = user, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var first = Find(await Body(login), "refreshToken");
        Assert.False(string.IsNullOrEmpty(first));

        var refresh = await client.PostAsJsonAsync($"{Base}/refresh-token", new { refreshToken = first });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var second = Find(await Body(refresh), "refreshToken");
        Assert.False(string.IsNullOrEmpty(second));
        Assert.NotEqual(first, second);

        // Reusing the first token must fail and (after the A1 fix) revoke the whole family, including `second`.
        var reuse = await client.PostAsJsonAsync($"{Base}/refresh-token", new { refreshToken = first });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var afterReuse = await client.PostAsJsonAsync($"{Base}/refresh-token", new { refreshToken = second });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsRejected()
    {
        var client = factory.CreateClient();
        var user = $"user_{Guid.NewGuid():N}"[..20];
        await client.PostAsJsonAsync($"{Base}/register", new { username = user, email = $"{user}@example.com", password = "Sup3r!Secret-test" });

        var login = await client.PostAsJsonAsync($"{Base}/login", new { identifier = user, password = "Wr0ng!Password" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}
