namespace OmniCore.Services.Auth.Domain.Enums;

/// <summary>
/// Specifies the intended scope and action of a short-lived security token.
/// </summary>
/// <remarks>
/// Enforcing token types prevents security exploits where a user attempts to use 
/// an Email Verification code to reset a Password or perform an unauthorized action.
/// </remarks>
public enum SecurityTokenType
{
    /// <summary>
    /// Token used to verify ownership of a newly registered email address.
    /// </summary>
    EmailVerification = 1,

    /// <summary>
    /// Single-use token generated when a user requests a password reset.
    /// </summary>
    PasswordReset = 2,

    /// <summary>
    /// One-time passcode issued for step-up or passwordless authentication.
    /// </summary>
    LoginOtp = 3,

    /// <summary>
    /// Verification code sent to authorize an account email address update.
    /// </summary>
    ChangeEmail = 4
}