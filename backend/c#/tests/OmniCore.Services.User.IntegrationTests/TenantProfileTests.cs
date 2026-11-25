using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Services.User.Application.Features.Profiles.Commands.ProvisionUserProfile;
using OmniCore.Services.User.Infrastructure.Persistence.Contexts;

namespace OmniCore.Services.User.IntegrationTests;

/// <summary>ADR 0001 / plan 0007 step 3: the same person has a separate profile in each tenant and cannot see another's.</summary>
[Collection(UserApiCollection.Name)]
public class TenantProfileTests(UserApiFactory factory)
{
    private const string Url = "/api/v1/users/me/tenant-profile";

    private static string Token(Guid id, Guid? tenant)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, id.ToString()), new(ClaimTypes.NameIdentifier, id.ToString()) };
        if (tenant is not null) claims.Add(new Claim("tenant_id", tenant.Value.ToString()));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(UserApiFactory.Secret));
        var jwt = new JwtSecurityToken(UserApiFactory.Issuer, UserApiFactory.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private HttpClient ClientFor(Guid id, Guid? tenant)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(id, tenant));
        return client;
    }

    private async Task<Guid> Provision()
    {
        var id = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var result = await sender.Send(new ProvisionUserProfileCommand(
            id, $"user_{id:N}"[..16], $"{id:N}"[..8] + "@example.com", null, null));
        Assert.True(result.IsSuccess);
        return id;
    }

    private static async Task<string?> GamerTag(HttpResponseMessage response)
    {
        var json = (await response.Content.ReadFromJsonAsync<JsonElement>())!;
        return Find(json, "gamerTag")?.GetString();
    }

    private static JsonElement? Find(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
            if (Find(p.Value, name) is { } nested) return nested;
        }
        return null;
    }

    [Fact]
    public async Task The_same_person_has_a_separate_profile_in_each_tenant()
    {
        var user = await Provision();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var inA = ClientFor(user, tenantA);
        var inB = ClientFor(user, tenantB);

        Assert.Equal(HttpStatusCode.NotFound, (await inA.GetAsync(Url)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await inA.PutAsJsonAsync(Url, new { gamerTag = "Alpha" })).StatusCode);

        // Tenant B cannot see A's row, even for the same user id.
        Assert.Equal(HttpStatusCode.NotFound, (await inB.GetAsync(Url)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await inB.PutAsJsonAsync(Url, new { gamerTag = "Bravo" })).StatusCode);

        Assert.Equal("Alpha", await GamerTag(await inA.GetAsync(Url)));
        Assert.Equal("Bravo", await GamerTag(await inB.GetAsync(Url)));

        // A second PUT in A updates the one row (the unique index would reject a duplicate).
        Assert.Equal(HttpStatusCode.OK, (await inA.PutAsJsonAsync(Url, new { gamerTag = "Alpha2" })).StatusCode);
        Assert.Equal("Alpha2", await GamerTag(await inA.GetAsync(Url)));
        Assert.Equal("Bravo", await GamerTag(await inB.GetAsync(Url)));
    }

    [Fact]
    public async Task A_token_without_a_tenant_is_forbidden()
    {
        var user = await Provision();
        var client = ClientFor(user, tenant: null);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(Url, new { gamerTag = "Nope" })).StatusCode);
    }

    [Fact]
    public async Task Anonymous_calls_are_unauthorized_and_a_bad_tag_is_a_bad_request()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync(Url)).StatusCode);

        var client = ClientFor(await Provision(), Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(Url, new { gamerTag = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(Url, new { gamerTag = new string('x', 51) })).StatusCode);
    }

    [Fact]
    public async Task A_person_without_a_base_profile_gets_404_on_the_first_save()
    {
        var client = ClientFor(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(Url, new { gamerTag = "Ghost" })).StatusCode);
    }

    [Fact]
    public void MigrationSnapshot_MatchesTheModel()
    {
        // The migration and snapshot were written by hand; this fails if they drift apart from the model.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        Assert.False(db.Database.HasPendingModelChanges());
    }
}
