namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Shared.Domain.Abstractions;

public partial class Account
{
    /// <summary>
    /// Issues a single-use security token of the given type. Older unused tokens of the same type stop working,
    /// so only the newest link in a user's mailbox is valid (ADR 0009). Load <see cref="SecurityTokens"/> first.
    /// </summary>
    /// <param name="type">What the token is for; a token cannot be redeemed for another type.</param>
    /// <param name="codeHash">SHA-256 of the raw code (see <see cref="SecurityToken.HashCode"/>).</param>
    /// <param name="lifetime">How long the token stays valid.</param>
    public Result<SecurityToken> IssueSecurityToken(SecurityTokenType type, string codeHash, TimeSpan lifetime)
    {
        foreach (var older in SecurityTokens.Where(t => t.TokenType == type && !t.IsUsed && !t.IsExpired))
        {
            older.MarkAsUsed();
        }

        var created = SecurityToken.Create(Id, codeHash, type, lifetime);
        if (created.IsFailure)
        {
            return created;
        }

        SecurityTokens.Add(created.Value);
        return created;
    }

    /// <summary>
    /// Consumes the token that matches the hash and type. Unknown, used, expired and wrong-type tokens all fail with the
    /// same error code so a caller cannot tell them apart.
    /// </summary>
    public Result RedeemSecurityToken(SecurityTokenType type, string codeHash)
    {
        var token = SecurityTokens.FirstOrDefault(t => t.TokenType == type && t.CodeHash == codeHash);
        if (token is null || token.IsUsed || token.IsExpired)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "The link is invalid or has expired."));
        }

        return token.MarkAsUsed();
    }
}
