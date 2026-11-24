namespace OmniCore.Services.Auth.Infrastructure.Configs;

using OmniCore.Shared.Infrastructure.Configs;

/// <summary>Environment variable <c>Auth__RequireVerifiedEmail</c> (ADR 0010). Off by default.</summary>
public sealed class AuthPolicyConfig : ConfigBase
{
    public override string SectionName => "Auth";

    public bool RequireVerifiedEmail { get; init; }
}
