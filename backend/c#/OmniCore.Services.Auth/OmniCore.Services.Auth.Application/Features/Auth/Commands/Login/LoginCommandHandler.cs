namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Login;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Mappings;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record LoginCommand(
    string Identifier, // Email or Username
    string Password) : ICommand<AuthResponse>;

public sealed class LoginCommandHandler(
    IAccountRepository accountRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    ICacheService cacheService) : ICommandHandler<LoginCommand, AuthResponse>
{
    private static readonly TimeSpan AccountCacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Normalize identifier & construct cache key
        var normalizedIdentifier = request.Identifier.Trim().ToLowerInvariant();
        var cacheKey = $"auth:account:{normalizedIdentifier}";

        // 2. Fetch Account DTO via Cache (Redis -> Memory Fallback -> DB)
        var accountDto = await cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var account = await accountRepository.GetByIdentifierAsync(request.Identifier, ct);
                return account?.ToDto();
            },
            absoluteExpiration: AccountCacheTtl,
            cancellationToken: cancellationToken);

        // 2a. Account Existence Check
        if (accountDto is null)
        {
            return Result.Failure<AuthResponse>(
                Error.NotFound("Auth.AccountNotFound", $"No account found matching '{request.Identifier}'."));
        }

        // 2b. Account Active State Check
        if (!accountDto.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.AccountInactive", "Your account has been deactivated. Please contact support."));
        }

        // 3. Password Presence Check
        if (string.IsNullOrWhiteSpace(accountDto.PasswordHash))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.PasswordNotConfigured", "Account does not have a local password set. Try logging in via your external provider."));
        }

        // 4. Password Verification Check
        if (!passwordHasher.VerifyPassword(request.Password, accountDto.PasswordHash))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidPassword", "The password provided is incorrect."));
        }

        // 5. Generate Access Token from cached DTO
        var accessToken = jwtTokenService.GenerateToken(
            accountDto.Id,
            accountDto.Email,
            accountDto.Username,
            accountDto.Roles
        );

        // 6. Generate Refresh Token
        var refreshToken = refreshTokenService.GenerateToken(accountDto.Id);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}