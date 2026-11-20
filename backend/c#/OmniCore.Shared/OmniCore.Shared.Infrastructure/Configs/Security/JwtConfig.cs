namespace OmniCore.Shared.Infrastructure.Configs.Security;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;
using OmniCore.Shared.Infrastructure.Security;

/// <summary>
/// JWT settings (ADR 0003). Three roles, set by which values are present:
/// <list type="bullet">
/// <item>Auth service signing: <see cref="PrivateKeyPem"/> (RS256). Without it Auth signs HS256 with <see cref="Secret"/> (legacy).</item>
/// <item>Other services validating: <see cref="JwksUrl"/> (public keys fetched from Auth). Auth itself validates with its own public key.</item>
/// <item>Cut-over window: while <see cref="Secret"/> is set, HS256 tokens are still accepted. Remove it to reject them.</item>
/// </list>
/// </summary>
public class JwtConfig : ConfigBase, IValidatableObject
{
    public override string SectionName => "Jwt";

    /// <summary>Legacy shared HS256 secret. Optional: leave unset once every service uses RS256 (it then stops being accepted).</summary>
    public string? Secret { get; init; }

    /// <summary>RSA private key in PEM (2048 bits or more). Only the Auth service gets this. Newlines may be written as \n.</summary>
    public string? PrivateKeyPem { get; init; }

    /// <summary>Public key (PEM) of the previous signing key, published in the JWKS and accepted while old tokens expire (key rotation).</summary>
    public string? PreviousPublicKeyPem { get; init; }

    /// <summary>Optional key id for the current key. Derived from the public key when empty.</summary>
    public string? KeyId { get; init; }

    /// <summary>URL of the Auth service JWKS, for services that only validate tokens.</summary>
    public string? JwksUrl { get; init; }

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    [Range(1, 1440, ErrorMessage = "ExpiryMinutes must be between 1 and 1440 minutes.")]
    public int ExpiryMinutes { get; init; } = 15;

    [Range(1, 90, ErrorMessage = "RefreshTokenExpiryDays must be between 1 and 90 days.")]
    public int RefreshTokenExpiryDays { get; init; } = 7;

    /// <summary>Max active refresh tokens (sessions) per account; the oldest are revoked on a new login. 0 = no cap (ADR 0003, A10).</summary>
    [Range(0, 100, ErrorMessage = "MaxActiveSessions must be between 0 (no cap) and 100.")]
    public int MaxActiveSessions { get; init; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasSecret = !string.IsNullOrWhiteSpace(Secret);
        if (hasSecret && Secret!.Length < 32)
            yield return new ValidationResult("JWT Secret must be at least 32 characters.", [nameof(Secret)]);

        if (!hasSecret && string.IsNullOrWhiteSpace(PrivateKeyPem) && string.IsNullOrWhiteSpace(JwksUrl))
            yield return new ValidationResult(
                "Set Jwt:PrivateKeyPem (Auth), Jwt:JwksUrl (other services) or, for the legacy mode, Jwt:Secret.");

        if (!string.IsNullOrWhiteSpace(JwksUrl) && !Uri.TryCreate(JwksUrl, UriKind.Absolute, out _))
            yield return new ValidationResult("Jwt:JwksUrl must be an absolute URL.", [nameof(JwksUrl)]);

        string? privateError = null, previousError = null;
        if (!string.IsNullOrWhiteSpace(PrivateKeyPem))
            privateError = JwtKeySet.TryValidatePem(PrivateKeyPem, requirePrivate: true);
        if (!string.IsNullOrWhiteSpace(PreviousPublicKeyPem))
            previousError = JwtKeySet.TryValidatePem(PreviousPublicKeyPem, requirePrivate: false);
        if (privateError is not null)
            yield return new ValidationResult($"Jwt:PrivateKeyPem is not usable: {privateError}", [nameof(PrivateKeyPem)]);
        if (previousError is not null)
            yield return new ValidationResult($"Jwt:PreviousPublicKeyPem is not usable: {previousError}", [nameof(PreviousPublicKeyPem)]);
    }
}
