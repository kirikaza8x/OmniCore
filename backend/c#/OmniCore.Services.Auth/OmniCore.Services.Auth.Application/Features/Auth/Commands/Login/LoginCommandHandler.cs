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
    internal static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid username, email or password.");

    private static string? _dummyHash;

    // A real bcrypt hash of a random value, made once, so "unknown account" does the same work as "wrong password".
    private static string DummyHash(IPasswordHasherService hasher) =>
        _dummyHash ??= hasher.HashPassword(Guid.NewGuid().ToString("N"));

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load the Account aggregate (refresh tokens + roles included) straight from the database.
        //    The account used to be cached in Redis as a DTO that included the password hash for 10 minutes;
        //    a password hash does not belong in a shared cache and the entry went stale after a password change.
        var account = await accountRepository.GetByIdentifierAsync(request.Identifier.Trim(), cancellationToken);

        // 2. One answer for every way a login can fail (A9). The caller cannot tell "no such account", "deactivated",
        //    "no local password" and "wrong password" apart, neither by the message nor by the response time: when there
        //    is nothing to compare with, a throw-away hash is checked so the failure costs the same.
        if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash?.Value))
        {
            passwordHasher.VerifyPassword(request.Password, DummyHash(passwordHasher));
            return Result.Failure<AuthResponse>(InvalidCredentials);
        }

        var passwordMatches = passwordHasher.VerifyPassword(request.Password, account.PasswordHash!.Value);

        // A deactivated account is rejected only after the password check, with the same answer.
        if (!passwordMatches || !account.IsActive)
        {
            return Result.Failure<AuthResponse>(InvalidCredentials);
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