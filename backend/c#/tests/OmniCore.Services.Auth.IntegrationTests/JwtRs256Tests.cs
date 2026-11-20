using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>ADR 0003 step 4: access tokens are RS256, the JWKS publishes only public keys, forged tokens are rejected.</summary>
public class JwtRs256Tests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private const string Base = "/api/v1/auth";
    private const string Issuer = "omnicore-tests";
    private const string Audience = "omnicore-tests";

    private static string? FindString(JsonElement e, string name)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        foreach (var p in e.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.String)
                return p.Value.GetString();
            var nested = FindString(p.Value, name);
            if (nested is not null) return nested;
        }
        return null;
    }

    private async Task<string> LoginAndGetAccessToken(HttpClient client)
    {
        var user = $"user_{Guid.NewGuid():N}"[..20];
        const string password = "Sup3r!Secret-test";
        await client.PostAsJsonAsync($"{Base}/register", new { username = user, email = $"{user}@example.com", password });
        var login = await client.PostAsJsonAsync($"{Base}/login", new { identifier = user, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return FindString((await login.Content.ReadFromJsonAsync<JsonElement>())!, "accessToken")!;
    }

    private static async Task<HttpStatusCode> CallMe(HttpClient client, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Base}/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.SendAsync(request)).StatusCode;
    }

    private static string Sign(SecurityKey key, string alg, string? kid = null)
    {
        if (kid is not null) key.KeyId = kid;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(key, alg)
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    [Fact]
    public async Task AccessToken_IsRs256WithAKeyId_AndWorksOnAProtectedEndpoint()
    {
        var client = factory.CreateClient();
        var token = await LoginAndGetAccessToken(client);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("RS256", jwt.Header.Alg);
        Assert.False(string.IsNullOrEmpty(jwt.Header.Kid));
        Assert.Equal(HttpStatusCode.OK, await CallMe(client, token));
    }

    [Fact]
    public async Task Jwks_PublishesTheSigningKey_WithoutAnyPrivateParameter()
    {
        var client = factory.CreateClient();
        var token = await LoginAndGetAccessToken(client);
        var kid = new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Kid;

        var response = await client.GetAsync("/.well-known/jwks.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var key = doc.RootElement.GetProperty("keys").EnumerateArray().Single(k => k.GetProperty("kid").GetString() == kid);
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("RS256", key.GetProperty("alg").GetString());
        foreach (var privateParameter in new[] { "d", "p", "q", "dp", "dq", "qi" })
            Assert.False(key.TryGetProperty(privateParameter, out _), $"JWKS leaks private parameter {privateParameter}");

        // The published key really verifies the token (what a service in another language would do).
        var jwks = new JsonWebKeySet(json);
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer, ValidAudience = Audience, IssuerSigningKeys = jwks.Keys, ValidAlgorithms = ["RS256"]
        }, out _);
    }

    [Fact]
    public async Task TokenSignedWithAnotherRsaKey_IsRejected()
    {
        using var other = RSA.Create(2048);
        var forged = Sign(new RsaSecurityKey(other), SecurityAlgorithms.RsaSha256, "attacker");
        Assert.Equal(HttpStatusCode.Unauthorized, await CallMe(factory.CreateClient(), forged));
    }

    [Fact]
    public async Task Hs256Token_IsRejected_AfterTheCutOver()
    {
        // The old shared secret no longer works: Jwt:Secret is unset in this API.
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("CHANGE_ME_test_only_secret_at_least_32_chars"));
        var forged = Sign(secret, SecurityAlgorithms.HmacSha256);
        Assert.Equal(HttpStatusCode.Unauthorized, await CallMe(factory.CreateClient(), forged));
    }

    [Fact]
    public async Task Hs256TokenSignedWithThePublicKeyAsSecret_IsRejected()
    {
        // The classic algorithm-confusion attack: use the published public key bytes as an HMAC secret.
        var publicDer = AuthApiFactory.SigningKey.ExportSubjectPublicKeyInfo();
        var forged = Sign(new SymmetricSecurityKey(publicDer.Length >= 32 ? publicDer : new byte[32]), SecurityAlgorithms.HmacSha256);
        Assert.Equal(HttpStatusCode.Unauthorized, await CallMe(factory.CreateClient(), forged));
    }

    [Fact]
    public async Task UnsignedToken_AlgNone_IsRejected()
    {
        static string B64(string s) => Base64UrlEncoder.Encode(s);
        var none = $"{B64("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{B64($"{{\"sub\":\"{Guid.NewGuid()}\",\"iss\":\"{Issuer}\",\"aud\":\"{Audience}\",\"exp\":{DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}}}")}.";
        Assert.Equal(HttpStatusCode.Unauthorized, await CallMe(factory.CreateClient(), none));
    }
}
