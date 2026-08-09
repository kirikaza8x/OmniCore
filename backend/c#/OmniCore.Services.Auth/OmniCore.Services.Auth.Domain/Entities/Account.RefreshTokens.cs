namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Shared.Domain.Abstractions;

public partial class Account
{
    /// <summary>
    /// Generates and attaches a new refresh token, optionally enforcing a maximum active session limit.
    /// </summary>
    /// <param name="token">The cryptographic token string.</param>
    /// <param name="duration">The lifetime duration of the token.</param>
    /// <param name="maxActiveSessions">Optional maximum allowed active refresh tokens per account. If null, no capping is applied.</param>
    /// <returns>A result containing the created <see cref="RefreshToken"/> or a validation error.</returns>
    public Result<RefreshToken> AddRefreshToken(
        string token, 
        TimeSpan duration, 
        int? maxActiveSessions = null)
    {
        if (maxActiveSessions.HasValue)
        {
            var activeTokens = _refreshTokens
                .Where(t => t.IsActive)
                .OrderBy(t => t.CreatedAtUtc)
                .ToList();

            if (activeTokens.Count >= maxActiveSessions.Value)
            {
                var tokensToRevoke = activeTokens.Take(activeTokens.Count - maxActiveSessions.Value + 1);
                foreach (var oldToken in tokensToRevoke)
                {
                    oldToken.Revoke();
                }
            }
        }

        var tokenResult = RefreshToken.Create(Id, token, duration);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<RefreshToken>(tokenResult.Error);
        }

        _refreshTokens.Add(tokenResult.Value);
        return Result.Success(tokenResult.Value);
    }

    /// <summary>
    /// Rotates an existing refresh token with a new token, triggering breach revocation if token reuse is detected.
    /// </summary>
    /// <param name="currentToken">The raw token string being presented for refresh.</param>
    /// <param name="newToken">The replacement token string.</param>
    /// <param name="newDuration">The lifetime duration for the new token.</param>
    /// <returns>A result containing the newly created <see cref="RefreshToken"/> or an authorization failure error.</returns>
    public Result<RefreshToken> RotateRefreshToken(
        string currentToken, 
        string newToken, 
        TimeSpan newDuration)
    {
        var existingToken = _refreshTokens.FirstOrDefault(t => t.Token == currentToken);

        if (existingToken is null)
        {
            return Result.Failure<RefreshToken>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token was not found."));
        }

        if (existingToken.IsRevoked)
        {
            RevokeAllRefreshTokens();
            return Result.Failure<RefreshToken>(
                Error.Unauthorized(
                    "Auth.TokenReuseDetected", 
                    "Security threat detected: Revoked token reuse attempt. All active sessions have been invalidated."));
        }

        if (existingToken.IsExpired)
        {
            return Result.Failure<RefreshToken>(
                Error.Unauthorized("Auth.ExpiredRefreshToken", "Refresh token has expired."));
        }

        var revokeResult = existingToken.Revoke(replacedByToken: newToken);
        if (revokeResult.IsFailure)
        {
            return Result.Failure<RefreshToken>(revokeResult.Error);
        }

        return AddRefreshToken(newToken, newDuration);
    }

    /// <summary>
    /// Revokes a specific active refresh token by its raw string value.
    /// </summary>
    /// <param name="token">The token string to revoke.</param>
    /// <returns>A result indicating success or a failure if not found.</returns>
    public Result RevokeRefreshToken(string token)
    {
        var existingToken = _refreshTokens.FirstOrDefault(t => t.Token == token);

        if (existingToken is null)
        {
            return Result.Failure(Error.NotFound("Auth.TokenNotFound", "Refresh token was not found."));
        }

        if (existingToken.IsRevoked)
        {
            return Result.Success();
        }

        return existingToken.Revoke();
    }

    /// <summary>
    /// Invalidates and revokes all currently active refresh tokens associated with this account.
    /// </summary>
    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }
}