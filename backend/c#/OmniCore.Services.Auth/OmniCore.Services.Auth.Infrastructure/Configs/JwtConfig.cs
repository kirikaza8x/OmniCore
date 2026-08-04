namespace OmniCore.Services.Auth.Infrastructure.Configs;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

public class JwtConfig : ConfigBase
{
    public override string SectionName => "JwtConfigs";

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
}