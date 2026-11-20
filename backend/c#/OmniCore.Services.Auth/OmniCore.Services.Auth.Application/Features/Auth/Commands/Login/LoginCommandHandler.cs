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
    IPasswordHasherService passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load the Account aggregate (refresh tokens + roles included) straight from the database.
        //    The account used to be cached in Redis as a DTO that included the password hash for 10 minutes;
        //    a password hash does not belong in a shared cache and the entry went stale after a password change.
        var account = await accountRepository.GetByIdentifierAsync(request.Identifier.Trim(), cancellationToken);

        // 2. Account Existence Check
        if (account is null)
        {
            return Result.Failure<AuthResponse>(
                Error.NotFound("Auth.AccountNotFound", $"No account found matching '{request.Identifier}'."));
        }

        // 3. Account Active State Check
        if (!account.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.AccountInactive", "Your account has been deactivated. Please contact support."));
        }

        // 4. Password Presence Check
        if (string.IsNullOrWhiteSpace(account.PasswordHash?.Value))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.PasswordNotConfigured", "Account does not have a local password set. Try logging in via your external provider."));
        }

        // 5. Password Verification Check
        if (!passwordHasher.VerifyPassword(request.Password, account.PasswordHash!.Value))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidPassword", "The password provided is incorrect."));
        }

        // 6. Generate Refresh Token via Aggregate Root
        var (refreshTokenString, duration) = refreshTokenService.GenerateRefreshToken();
        var refreshTokenResult = account.AddRefreshToken(refreshTokenString, duration, refreshTokenService.MaxActiveSessions);
        if (refreshTokenResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(refreshTokenResult.Error);
        }

        // 7. Extract Role Names
        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        // 8. Generate JWT Access Token
        var accessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles
        );

        return Result.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}