namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Register;

using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public record RegisterCommand(
    string Username,
    string Email,
    string Password) : ICommand<AuthResponse>;

public sealed class RegisterCommandHandler(
    IAccountRepository accountRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var (isEmailTaken, isUsernameTaken) = await accountRepository.CheckUniquenessAsync(
            request.Email,
            request.Username,
            cancellationToken);

        if (isEmailTaken)
        {
            return Result.Failure<AuthResponse>(
                Error.Conflict("Account.EmailAlreadyInUse", "Email address is already registered."));
        }

        if (isUsernameTaken)
        {
            return Result.Failure<AuthResponse>(
                Error.Conflict("Account.UsernameAlreadyInUse", "Username is already taken."));
        }

        // 2. Hash Password
        var passwordHash = passwordHasher.HashPassword(request.Password);

        // 3. Create Account Aggregate Root
        var accountResult = Account.Create(
            rawUsername: request.Username,
            rawEmail: request.Email,
            rawPasswordHash: passwordHash
        );

        if (accountResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(accountResult.Error);
        }

        var account = accountResult.Value;

        // 4. Save Account Entity via synchronous Add()
        accountRepository.Add(account);

        // 5. Extract Roles (empty/default for new accounts)
        var roles = account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r));

        // 6. Generate Access Token & Refresh Token for auto-login
        var accessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles
        );

        var refreshToken = refreshTokenService.GenerateToken(account.Id.Value);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // UoW Behavior automatically commits changes on Result.Success
        return Result.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}