namespace OmniCore.Shared.Infrastructure.Configs.Security;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

public class JwtConfig : ConfigBase
{
    public override string SectionName => "Jwt";

    [Required, MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters.")]
    public string Secret { get; init; } = default!;

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
}