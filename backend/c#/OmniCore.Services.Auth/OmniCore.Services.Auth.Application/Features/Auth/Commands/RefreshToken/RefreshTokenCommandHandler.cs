namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.RefreshToken;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record RefreshTokenCommand(
    string RefreshToken) : ICommand<AuthResponse>;

public sealed class RefreshTokenCommandHandler(
    IAccountRepository accountRepository,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Retrieve Account aggregate containing the active refresh token
        var account = await accountRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid, expired, or revoked."));
        }

        // 2. Generate new cryptographic refresh token details
        var (newRefreshTokenString, duration) = refreshTokenService.GenerateRefreshToken();

        // 3. Rotate Refresh Token via Aggregate Root (revokes existing, issues new)
        var rotateResult = account.RotateRefreshToken(request.RefreshToken, newRefreshTokenString, duration);
        if (rotateResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(rotateResult.Error);
        }

        // 4. Extract Roles & Generate New JWT Access Token
        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        var newAccessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles
        );

        return Result.Success(new AuthResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}