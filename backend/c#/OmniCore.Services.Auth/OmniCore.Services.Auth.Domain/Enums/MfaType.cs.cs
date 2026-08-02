namespace OmniCore.Services.Auth.Domain.Enums;

/// <summary>
/// Defines the supported Multi-Factor Authentication (MFA) mechanisms.
/// </summary>
/// <remarks>
/// Storing MFA type as an explicit enum allows the domain logic to apply 
/// strategy-specific verification rules during two-factor challenges.
/// </remarks>
public enum MfaType
{
    /// <summary>
    /// Time-based One-Time Password generated via Authenticator apps (e.g., Google Authenticator, 1Password).
    /// </summary>
    Totp = 1,

    /// <summary>
    /// One-time numeric passcode sent via Short Message Service (SMS).
    /// </summary>
    Sms = 2,

    /// <summary>
    /// FIDO2 / WebAuthn cryptographic hardware credential or passkey.
    /// </summary>
    Passkey = 3
}