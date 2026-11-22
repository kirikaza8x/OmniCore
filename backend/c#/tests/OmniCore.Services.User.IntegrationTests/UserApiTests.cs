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
using OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;

namespace OmniCore.Services.User.IntegrationTests;

[Collection(UserApiCollection.Name)]
public class UserApiTests(UserApiFactory factory)
{
    private const string Base = "/api/v1/users";

    private static string Token(Guid id, params string[] roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, id.ToString()), new(ClaimTypes.NameIdentifier, id.ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(UserApiFactory.Secret));
        var jwt = new JwtSecurityToken(UserApiFactory.Issuer, UserApiFactory.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private HttpClient ClientFor(Guid id, params string[] roles)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(id, roles));
        return client;
    }

    private async Task<Guid> Provision(string? username = null)
    {
        var id = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var result = await sender.Send(new ProvisionUserProfileCommand(
            id, username ?? $"user_{id:N}"[..16], $"{id:N}"[..8] + "@example.com", null, null));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : "");
        return id;
    }

    // Finds a property anywhere in the (ApiResult-wrapped) JSON, case-insensitive.
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

    [Fact]
    public async Task Endpoints_need_a_token()
    {
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Base}/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Base}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Base}/me/addresses")).StatusCode);
    }

    [Fact]
    public async Task A_signed_in_user_without_a_profile_gets_404()
    {
        var response = await ClientFor(Guid.NewGuid()).GetAsync($"{Base}/me");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_and_updates_the_profile_and_settings()
    {
        var id = await Provision();
        var client = ClientFor(id);

        var get = await client.GetAsync($"{Base}/me");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(id.ToString(), Find(await Body(get), "id")!.Value.GetString());

        var put = await client.PutAsJsonAsync($"{Base}/me", new { firstName = "Ann", lastName = "Lee", bio = "hi" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var again = await Body(await client.GetAsync($"{Base}/me"));
        Assert.Equal("Ann", Find(again, "firstName")!.Value.GetString());

        var settings = await client.PutAsJsonAsync($"{Base}/me/settings",
            new { theme = "Dark", language = "vi", emailNotifications = false, pushNotifications = true });
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);

        var bad = await client.PutAsJsonAsync($"{Base}/me/settings",
            new { theme = "Purple", language = "vi", emailNotifications = false, pushNotifications = true });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Addresses_can_be_added_changed_and_removed_and_exactly_one_stays_primary()
    {
        var client = ClientFor(await Provision());
        object Addr(string line, bool primary) => new { addressLine1 = line, city = "Hanoi", country = "Vietnam", isPrimary = primary };

        var first = await client.PostAsJsonAsync($"{Base}/me/addresses", Addr("1 Main St", false));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstId = Find(await Body(first), "id")!.Value.GetString();

        var second = await client.PostAsJsonAsync($"{Base}/me/addresses", Addr("2 Side St", true));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var list = await Body(await client.GetAsync($"{Base}/me/addresses"));
        var items = Find(list, "data")?.ValueKind == JsonValueKind.Array ? Find(list, "data")!.Value : list;
        var primaries = items.EnumerateArray().Count(a => a.GetProperty("isPrimary").GetBoolean());
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(1, primaries);

        var update = await client.PutAsJsonAsync($"{Base}/me/addresses/{firstId}", Addr("1 Main St, floor 2", false));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await client.DeleteAsync($"{Base}/me/addresses/{firstId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{Base}/me/addresses/{firstId}")).StatusCode);
    }

    [Fact]
    public async Task The_public_profile_hides_private_data_and_details_is_admin_only()
    {
        var target = await Provision();
        var caller = ClientFor(Guid.NewGuid());

        var pub = await caller.GetAsync($"{Base}/{target}");
        Assert.Equal(HttpStatusCode.OK, pub.StatusCode);
        var json = await Body(pub);
        Assert.Null(Find(json, "email"));
        Assert.Null(Find(json, "phoneNumber"));

        Assert.Equal(HttpStatusCode.Forbidden, (await caller.GetAsync($"{Base}/{target}/details")).StatusCode);
        var admin = ClientFor(Guid.NewGuid(), "Admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Base}/{target}/details")).StatusCode);
    }

    [Fact]
    public async Task A_profile_is_created_from_the_Auth_registration_event()
    {
        var id = Guid.NewGuid();
        var username = $"evt_{id:N}"[..14];
        using (var scope = factory.Services.CreateScope())
        {
            // IBus publishes straight to the broker (the outbox belongs to IPublishEndpoint inside a unit of work).
            await scope.ServiceProvider.GetRequiredService<IBus>()
                .Publish(new AccountRegisteredIntegrationEvent(id, username, $"{username}@example.com", username, ""));
        }

        var client = ClientFor(id);
        HttpStatusCode last = default;
        for (var i = 0; i < 40; i++)
        {
            last = (await client.GetAsync($"{Base}/me")).StatusCode;
            if (last == HttpStatusCode.OK) break;
            await Task.Delay(500);
        }
        Assert.Equal(HttpStatusCode.OK, last);
    }
}
