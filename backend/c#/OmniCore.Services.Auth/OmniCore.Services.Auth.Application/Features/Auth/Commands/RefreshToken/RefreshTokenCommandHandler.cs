namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.RefreshToken;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record RefreshTokenCommand(
    string RefreshToken) : ICommand<AuthResponse>;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IAccountRepository accountRepository,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Retrieve Refresh Token Entity
        var existingToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !refreshTokenService.ValidateToken(existingToken))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid, expired, or revoked."));
        }

        // 2. Get Associated Account
        var account = await accountRepository.GetByIdAsync(existingToken.AccountId, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.NotFound("Account.NotFound", "Associated account was not found or is inactive."));
        }

        // 3. Rotate Refresh Token (Create new, revoke existing)
        var newRefreshToken = refreshTokenService.GenerateToken(account.Id.Value);
        existingToken.Revoke(replacedByToken: newRefreshToken.Token);

        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // 4. Extract Roles & Generate New JWT Access Token (Unwrapping Value Objects)
        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        var newAccessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles
        );

        // UoW Behavior auto-commits on Result.Success
        return Result.Success(new AuthResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}