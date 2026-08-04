namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Login;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Domain.Repositories;
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
    IRefreshTokenService refreshTokenService) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Fetch Account by Identifier
        var account = await accountRepository.GetByIdentifierAsync(request.Identifier, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials."));
        }

        // 2. Validate Password Presence (OAuth accounts won't have a password hash)
        if (account.PasswordHash is null || string.IsNullOrWhiteSpace(account.PasswordHash.Value))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials."));
        }

        // 3. Verify Password against Value Object string value
        if (!passwordHasher.VerifyPassword(request.Password, account.PasswordHash.Value))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials."));
        }

        // 4. Extract Roles from AccountRoles collection
        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        // 5. Generate Access Token (unwrapping Value Objects)
        var accessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles
        );

        // 6. Generate Refresh Token
        var refreshToken = refreshTokenService.GenerateToken(account.Id.Value);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}