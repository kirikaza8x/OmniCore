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
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : ICommandHandler<RegisterCommand, AuthResponse>
{
    private const string DefaultRoleName = "User";

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

        // 4. Assign Default Role
        var defaultRole = await roleRepository.GetByNameAsync(DefaultRoleName, cancellationToken);
        if (defaultRole is null)
        {
            return Result.Failure<AuthResponse>(
                Error.NotFound("Role.DefaultRoleNotFound", $"Default role '{DefaultRoleName}' was not found."));
        }

        var assignRoleResult = account.AssignRole(defaultRole.Id);
        if (assignRoleResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(assignRoleResult.Error);
        }

        // 5. Attach Refresh Token to Aggregate Root
        var (refreshTokenString, duration) = refreshTokenService.GenerateRefreshToken();
        var refreshTokenResult = account.AddRefreshToken(refreshTokenString, duration);
        if (refreshTokenResult.IsFailure)
        {
            return Result.Failure<AuthResponse>(refreshTokenResult.Error);
        }

        // 6. Save Account Entity & graph via Repository
        accountRepository.Add(account);

        // 7. Extract Roles for JWT Generation
        var roles = new[] { defaultRole.Name };

        // 8. Generate Access Token containing role claims
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