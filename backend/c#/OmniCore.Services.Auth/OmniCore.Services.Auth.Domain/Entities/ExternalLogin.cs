using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Links an Account to an external OAuth identity provider (e.g., Google, GitHub, Microsoft).
/// </summary>
/// <remarks>
/// Allows passwordless single sign-on (SSO) integration across multiple third-party identity providers.
/// </remarks>
public class ExternalLogin : Entity<ExternalLoginId>
{
    public AccountId AccountId { get; private set; } = null!;

    /// <summary>Gets the name of the OAuth provider (e.g., "Google").</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Gets the unique subject identifier (`sub`) issued by the provider.</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public Account Account { get; private set; } = null!;

    private ExternalLogin() { }

    private ExternalLogin(ExternalLoginId id, AccountId accountId, string provider, string providerKey) : base(id)
    {
        AccountId = accountId;
        Provider = provider;
        ProviderKey = providerKey;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to link an external provider credential to an Account.
    /// </summary>
    /// <param name="accountId">The target account identifier.</param>
    /// <param name="provider">The identity provider name.</param>
    /// <param name="providerKey">The provider's unique user identifier.</param>
    /// <returns>A <see cref="Result{ExternalLogin}"/> containing the link entity or a validation failure.</returns>
    public static Result<ExternalLogin> Create(AccountId accountId, string provider, string providerKey)
    {
        if (accountId is null)
        {
            return Result.Failure<ExternalLogin>(Error.Validation("ExternalLogin.AccountIdRequired", "Account ID is required."));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result.Failure<ExternalLogin>(Error.Validation("ExternalLogin.ProviderEmpty", "Provider name cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            return Result.Failure<ExternalLogin>(Error.Validation("ExternalLogin.ProviderKeyEmpty", "Provider key cannot be empty."));
        }

        return new ExternalLogin(ExternalLoginId.New(), accountId, provider.Trim(), providerKey.Trim());
    }
}