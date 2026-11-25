namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.RefreshToken;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Tenants;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.Repositories;

/// <param name="TenantId">Optional: the tenant the caller was working in. Used only if the account is a member of it.</param>
public record RefreshTokenCommand(
    string RefreshToken,
    Guid? TenantId = null) : ICommand<AuthResponse>;

public sealed class RefreshTokenCommandHandler(
    IAccountRepository accountRepository,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork,
    ITenantRepository tenantRepository) : ICommandHandler<RefreshTokenCommand, AuthResponse>
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
            // UnitOfWorkBehavior only saves on success, so the "revoke all sessions" done by the aggregate on a
            // reused token would be lost. Persist it explicitly before returning the failure.
            if (rotateResult.Error.Code == "Auth.TokenReuseDetected")
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Failure<AuthResponse>(rotateResult.Error);
        }

        // 4. Extract Roles & Generate New JWT Access Token
        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        var memberships = await tenantRepository.GetMembershipsAsync(account.Id, cancellationToken);
        var tenant = TenantSelector.ToClaim(TenantSelector.Choose(memberships, request.TenantId));

        var newAccessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles,
            tenant
        );

        return Result.Success(new AuthResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}