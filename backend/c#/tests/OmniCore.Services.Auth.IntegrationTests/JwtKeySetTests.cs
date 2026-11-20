using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Shared.Infrastructure.Configs.Security;
using OmniCore.Shared.Infrastructure.Security;

namespace OmniCore.Services.Auth.IntegrationTests;

/// <summary>Key handling of ADR 0003 step 4 without a database: which algorithms are accepted in each mode, the remote JWKS fetch, startup validation.</summary>
public class JwtKeySetTests
{
    private const string LongSecret = "CHANGE_ME_test_only_secret_at_least_32_chars";

    private static JwtKeySet Build(JwtConfig config) => new(Options.Create(config), NullLogger<JwtKeySet>.Instance);

    private static string NewPrivatePem() => RSA.Create(2048).ExportPkcs8PrivateKeyPem();

    private static JwtConfig Config(string? secret = null, string? pem = null, string? jwks = null, string? previous = null) => new()
    {
        Issuer = "i", Audience = "a", Secret = secret, PrivateKeyPem = pem, JwksUrl = jwks, PreviousPublicKeyPem = previous
    };

    [Fact]
    public void LegacyMode_AcceptsOnlyHs256_AndSignsWithTheSecret()
    {
        var keys = Build(Config(secret: LongSecret));
        Assert.Equal(["HS256"], keys.ValidAlgorithms);
        Assert.Null(keys.Signing);
        Assert.NotNull(keys.Hs256Key);
    }

    [Fact]
    public void CutOverWindow_AcceptsBoth_AndSignsRs256()
    {
        var keys = Build(Config(secret: LongSecret, pem: NewPrivatePem()));
        Assert.Equal(["RS256", "HS256"], keys.ValidAlgorithms);
        Assert.NotNull(keys.Signing);
    }

    [Fact]
    public void AfterCutOver_AcceptsOnlyRs256()
    {
        var keys = Build(Config(pem: NewPrivatePem()));
        Assert.Equal(["RS256"], keys.ValidAlgorithms);
        Assert.Null(keys.Hs256Key);
    }

    [Fact]
    public void Rotation_PublishesTheCurrentAndThePreviousKey_WithDifferentKids()
    {
        using var old = RSA.Create(2048);
        var keys = Build(Config(pem: NewPrivatePem(), previous: old.ExportSubjectPublicKeyInfoPem()));

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(keys.BuildJwks()));
        var kids = doc.RootElement.GetProperty("keys").EnumerateArray().Select(k => k.GetProperty("kid").GetString()).ToList();
        Assert.Equal(2, kids.Count);
        Assert.NotEqual(kids[0], kids[1]);
    }

    [Fact]
    public void PemWithEscapedNewlines_FromAnEnvironmentVariable_IsAccepted()
    {
        var oneLine = NewPrivatePem().Replace("\r", "").Replace("\n", "\\n");
        Assert.NotNull(Build(Config(pem: oneLine)).Signing);
    }

    [Fact]
    public void ValidatorOnlyService_FetchesTheKeysFromTheJwksUrl_AndVerifiesATokenFromAuth()
    {
        // "Auth": has the private key and serves the JWKS on a local port.
        var auth = Build(Config(pem: NewPrivatePem()));
        var jwksJson = JsonSerializer.Serialize(auth.BuildJwks());
        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        var serving = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                try
                {
                    var ctx = await listener.GetContextAsync();
                    var bytes = System.Text.Encoding.UTF8.GetBytes(jwksJson);
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.OutputStream.WriteAsync(bytes);
                    ctx.Response.Close();
                }
                catch { break; }
            }
        });

        // "Another service": no private key, only the URL.
        var validator = Build(Config(jwks: $"http://127.0.0.1:{port}/.well-known/jwks.json"));
        Assert.Equal(["RS256"], validator.ValidAlgorithms);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", "u1")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = "i", Audience = "a",
            SigningCredentials = new SigningCredentials(auth.Signing!, SecurityAlgorithms.RsaSha256)
        }));
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = "i", ValidAudience = "a", ValidAlgorithms = validator.ValidAlgorithms,
            IssuerSigningKeyResolver = (_, _, kid, _) => validator.ResolveKeys(kid)
        }, out _);
        Assert.NotNull(principal);

        listener.Stop();
        serving.Wait(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidatorOnlyService_WhenAuthIsUnreachable_ResolvesNoKeys_AndDoesNotThrow()
    {
        var validator = Build(Config(jwks: $"http://127.0.0.1:{FreePort()}/.well-known/jwks.json"));
        Assert.Empty(validator.ResolveKeys("any"));
    }

    [Fact]
    public void Config_NeedsASecretAPrivateKeyOrAJwksUrl()
    {
        Assert.NotEmpty(Config().Validate(null!));
        Assert.Empty(Config(jwks: "http://auth-api:8080/.well-known/jwks.json").Validate(null!));
    }

    [Fact]
    public void Config_RejectsAShortSecret_APublicKeyAsPrivateKey_AndAWeakRsaKey()
    {
        Assert.NotEmpty(Config(secret: "too-short").Validate(null!));

        using var rsa = RSA.Create(2048);
        Assert.NotEmpty(Config(pem: rsa.ExportSubjectPublicKeyInfoPem()).Validate(null!));

        using var weak = RSA.Create(1024);
        Assert.NotEmpty(Config(pem: weak.ExportPkcs8PrivateKeyPem()).Validate(null!));

        Assert.NotEmpty(Config(pem: "not a pem").Validate(null!));
        Assert.Empty(Config(pem: rsa.ExportPkcs8PrivateKeyPem()).Validate(null!));
    }

    private static int FreePort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
