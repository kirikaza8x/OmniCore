namespace OmniCore.Services.Auth.Application.Features.Auth.Commands;

/// <summary>How long e-mailed security links stay valid (ADR 0009).</summary>
public static class SecurityTokenLifetimes
{
    public static readonly TimeSpan PasswordReset = TimeSpan.FromHours(1);
    public static readonly TimeSpan EmailVerification = TimeSpan.FromHours(24);
}
