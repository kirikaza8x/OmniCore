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
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Uniqueness Checks
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

        // 3. Create Account Aggregate Root (Raises AccountCreatedDomainEvent)
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

        // 4. Attach Refresh Token to Aggregate Root
        var (refreshTokenString, duration) = refreshTokenService.GenerateRefreshToken();
        var refreshTokenResult = account.AddRefreshToken(refreshTokenString, duration);
        if (refreshTokenResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(refreshTokenResult.Error);
        }

        // 5. Persist Account (Dispatches AccountCreatedDomainEvent -> assigns default "User" role)
        accountRepository.Add(account);

        // 6. Generate Access Token containing default "User" role
        var accessToken = jwtTokenService.GenerateToken(
            account.Id.Value,
            account.Email?.Value,
            account.Username.Value,
            roles: ["User"]
        );

        return Result.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            ExpiresInMinutes: jwtTokenService.ExpiryMinutes
        ));
    }
}