namespace OmniCore.Shared.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Shared.Infrastructure.Configs.Security;

/// <summary>
/// All the keys a service needs to sign and validate JWTs (ADR 0003, step 4).
/// Signing: the RSA private key (Auth only). Validation: the local public keys (Auth), or the keys fetched from
/// the JWKS URL and cached (every other service), plus the legacy HS256 secret while it is still configured.
/// </summary>
public sealed class JwtKeySet
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MinRefreshGap = TimeSpan.FromSeconds(30);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    private readonly ILogger<JwtKeySet> _logger;
    private readonly string? _jwksUrl;
    private readonly List<RsaSecurityKey> _localPublic = [];
    private readonly SymmetricSecurityKey? _hs256;
    private readonly object _gate = new();
    private List<SecurityKey> _remote = [];
    private DateTime _remoteFetchedUtc = DateTime.MinValue;
    private DateTime _lastAttemptUtc = DateTime.MinValue;

    /// <summary>Private key Auth signs with, or null when Auth is still in the legacy HS256 mode / the service only validates.</summary>
    public RsaSecurityKey? Signing { get; }

    /// <summary>The legacy HS256 key, or null once the secret has been removed.</summary>
    public SymmetricSecurityKey? Hs256Key => _hs256;

    /// <summary>True when this service publishes keys (it holds the private key).</summary>
    public bool HasLocalRsaKeys => _localPublic.Count > 0;

    /// <summary>Algorithms accepted on incoming tokens. Anything else, including "none", is rejected.</summary>
    public string[] ValidAlgorithms { get; }

    public JwtKeySet(IOptions<JwtConfig> options, ILogger<JwtKeySet> logger)
    {
        _logger = logger;
        var c = options.Value;
        _jwksUrl = string.IsNullOrWhiteSpace(c.JwksUrl) ? null : c.JwksUrl;

        if (!string.IsNullOrWhiteSpace(c.PrivateKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(NormalizePem(c.PrivateKeyPem));
            var kid = string.IsNullOrWhiteSpace(c.KeyId) ? ComputeKid(rsa) : c.KeyId!;
            Signing = new RsaSecurityKey(rsa) { KeyId = kid };
            _localPublic.Add(new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = kid });
        }

        if (!string.IsNullOrWhiteSpace(c.PreviousPublicKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(NormalizePem(c.PreviousPublicKeyPem));
            _localPublic.Add(new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = ComputeKid(rsa) });
        }

        if (!string.IsNullOrWhiteSpace(c.Secret))
            _hs256 = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(c.Secret));

        var algs = new List<string>();
        if (_localPublic.Count > 0 || _jwksUrl is not null) algs.Add(SecurityAlgorithms.RsaSha256);
        if (_hs256 is not null) algs.Add(SecurityAlgorithms.HmacSha256);
        ValidAlgorithms = algs.ToArray();
    }

    /// <summary>Keys to try for a token with the given <c>kid</c> header (may be null).</summary>
    public IEnumerable<SecurityKey> ResolveKeys(string? kid)
    {
        var keys = new List<SecurityKey>();

        if (_localPublic.Count > 0)
            keys.AddRange(_localPublic.Where(k => kid is null || k.KeyId == kid));
        else if (_jwksUrl is not null)
            keys.AddRange(GetRemoteKeys(kid));

        if (_hs256 is not null) keys.Add(_hs256);
        return keys;
    }

    /// <summary>The JWKS document to publish: public parts only, never private parameters.</summary>
    public object BuildJwks() => new
    {
        keys = _localPublic.Select(k =>
        {
            var p = k.Rsa is not null ? k.Rsa.ExportParameters(false) : k.Parameters;
            return new
            {
                kty = "RSA",
                use = "sig",
                alg = SecurityAlgorithms.RsaSha256,
                kid = k.KeyId,
                n = Base64UrlEncoder.Encode(p.Modulus),
                e = Base64UrlEncoder.Encode(p.Exponent)
            };
        }).ToArray()
    };

    /// <summary>Returns an error text when the PEM cannot be used, or null when it is fine (used by startup validation).</summary>
    public static string? TryValidatePem(string pem, bool requirePrivate)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(NormalizePem(pem));
            if (rsa.KeySize < 2048) return $"RSA key is {rsa.KeySize} bits, 2048 or more is required.";
            if (requirePrivate)
            {
                try { rsa.ExportParameters(true); }
                catch (CryptographicException) { return "this is a public key, a private key is required."; }
            }
            return null;
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            return "not a valid RSA PEM.";
        }
    }

    // Environment variables cannot hold real newlines easily, so "\n" is accepted for them.
    private static string NormalizePem(string pem) => pem.Replace("\\n", "\n").Trim();

    private static string ComputeKid(RSA rsa)
    {
        var p = rsa.ExportParameters(false);
        var hash = SHA256.HashData([.. p.Modulus!, .. p.Exponent!]);
        return Base64UrlEncoder.Encode(hash)[..22];
    }

    private IEnumerable<SecurityKey> GetRemoteKeys(string? kid)
    {
        lock (_gate)
        {
            var now = DateTime.UtcNow;
            var stale = now - _remoteFetchedUtc > CacheTtl;
            var unknownKid = kid is not null && !_remote.Any(k => k.KeyId == kid);
            // Refresh when the cache is old, or a token names a key we do not know (rotation), but never more often than
            // every 30 seconds so a flood of bad tokens cannot turn into a flood of requests to Auth.
            if ((stale || unknownKid) && now - _lastAttemptUtc > MinRefreshGap)
                Refresh(now);

            return kid is null ? _remote.ToList() : _remote.Where(k => k.KeyId == kid).ToList();
        }
    }

    private void Refresh(DateTime now)
    {
        _lastAttemptUtc = now;
        try
        {
            // Blocking on purpose: the token-validation callback is synchronous. The result is cached, so this runs rarely.
            var json = Http.GetStringAsync(_jwksUrl!).GetAwaiter().GetResult();
            var set = new JsonWebKeySet(json);
            _remote = set.Keys
                .Where(k => k.Kty == JsonWebAlgorithmsKeyTypes.RSA && (k.Use is null || k.Use == "sig"))
                .Cast<SecurityKey>()
                .ToList();
            _remoteFetchedUtc = now;
            _logger.LogInformation("Fetched {Count} signing key(s) from {Url}.", _remote.Count, _jwksUrl);
        }
        catch (Exception ex)
        {
            // Keep serving the keys we already have; a short Auth outage must not log everybody out.
            _logger.LogWarning(ex, "Could not fetch the JWKS from {Url}; using the cached keys ({Count}).", _jwksUrl, _remote.Count);
        }
    }
}
