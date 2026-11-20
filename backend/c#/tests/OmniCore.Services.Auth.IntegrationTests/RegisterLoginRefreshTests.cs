using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;

namespace OmniCore.Services.Auth.IntegrationTests;

// Written without a .NET SDK or Docker: not compiled or run yet.
[Collection(AuthApiCollection.Name)]
public class RegisterLoginRefreshTests(AuthApiFactory factory)
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

    [Fact]
    public async Task ParallelRefreshesWithOneToken_NeverBothSucceed()
    {
        // ADR 0003 step 2 (A7): before the xmin concurrency token, parallel refreshes could both succeed and fork the session.
        var client = factory.CreateClient();
        var user = $"user_{Guid.NewGuid():N}"[..20];
        const string password = "Sup3r!Secret-test";
        await client.PostAsJsonAsync($"{Base}/register", new { username = user, email = $"{user}@example.com", password });
        var login = await client.PostAsJsonAsync($"{Base}/login", new { identifier = user, password });
        var token = Find(await Body(login), "refreshToken")!;

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            client.PostAsJsonAsync($"{Base}/refresh-token", new { refreshToken = token })));
        var codes = responses.Select(r => r.StatusCode).ToList();

        Assert.Equal(1, codes.Count(c => c == HttpStatusCode.OK));
        // The others lost the race (409) or arrived after it and were treated as a reuse (401); never a 500.
        Assert.All(codes.Where(c => c != HttpStatusCode.OK), c =>
            Assert.True(c is HttpStatusCode.Conflict or HttpStatusCode.Unauthorized, $"unexpected {(int)c}"));
    }

    [Fact]
    public void MigrationSnapshot_MatchesTheModel()
    {
        // The refresh token migration was written by hand; this fails if the snapshot and the model drift apart.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task RefreshTokens_AreNotStoredInPlainText()
    {
        var client = factory.CreateClient();
        var user = $"user_{Guid.NewGuid():N}"[..20];
        await client.PostAsJsonAsync($"{Base}/register", new { username = user, email = $"{user}@example.com", password = "Sup3r!Secret-test" });
        var login = await client.PostAsJsonAsync($"{Base}/login", new { identifier = user, password = "Sup3r!Secret-test" });
        var raw = Find(await Body(login), "refreshToken")!;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var stored = await db.Database.SqlQuery<string>($"SELECT token_hash AS \"Value\" FROM auth.refresh_tokens").ToListAsync();
        var plainMatches = await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM auth.refresh_tokens WHERE token = {raw}").SingleAsync();

        Assert.Contains(OmniCore.Services.Auth.Domain.Entities.RefreshToken.HashToken(raw), stored);
        Assert.Equal(0, plainMatches);
    }
}
