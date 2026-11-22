namespace OmniCore.Services.Auth.Infrastructure.Persistence.Seeds;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Seeds;

/// <summary>
/// Creates the first administrator (A15) so the admin endpoints are reachable on a fresh database.
/// It acts only when <c>Admin__Email</c>, <c>Admin__Username</c> and <c>Admin__Password</c> are all set and no account
/// holds the Admin or SuperAdmin role yet, so it runs once and is harmless to leave configured. It never promotes an
/// account that already exists: anyone can register with any e-mail, so promoting by e-mail would hand the role to
/// whoever registered first.
/// </summary>
public sealed class AdminSeeder(
    AuthDbContext dbContext,
    IAccountRepository accounts,
    IPasswordHasherService passwordHasher,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger) : IDataSeeder<Account>
{
    public const int MinPasswordLength = 12;
    public int Order => 2; // after RoleSeeder

    public async Task SeedAllAsync()
    {
        var email = configuration["Admin:Email"]?.Trim();
        var username = configuration["Admin:Username"]?.Trim();
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            logger.LogInformation("No first admin configured (Admin__Email, Admin__Username, Admin__Password). Skipping.");
            return;
        }

        var adminExists = await dbContext.AccountRoles.AnyAsync(ar => ar.Role.Name == "Admin" || ar.Role.Name == "SuperAdmin");
        if (adminExists)
        {
            logger.LogInformation("An administrator already exists. Skipping first-admin seed.");
            return;
        }

        if (password.Length < MinPasswordLength)
        {
            logger.LogError("Admin__Password is shorter than {Min} characters; the first admin was NOT created.", MinPasswordLength);
            return;
        }

        var (isEmailTaken, isUsernameTaken) = await accounts.CheckUniquenessAsync(email, username, CancellationToken.None);
        if (isEmailTaken || isUsernameTaken)
        {
            logger.LogWarning("An account with the configured admin e-mail or username already exists; it is not promoted automatically. " +
                              "Use other values or assign the role by hand.");
            return;
        }

        var roles = await dbContext.Roles.Where(r => r.Name == "User" || r.Name == "SuperAdmin").ToListAsync();
        var userRole = roles.FirstOrDefault(r => r.Name == "User");
        var superAdminRole = roles.FirstOrDefault(r => r.Name == "SuperAdmin");
        if (userRole is null || superAdminRole is null)
        {
            logger.LogError("Default roles are missing; the first admin was NOT created.");
            return;
        }

        var created = Account.Create(username, email, passwordHasher.HashPassword(password));
        if (created.IsFailure)
        {
            logger.LogError("The configured admin account is invalid ({Code}: {Description}); it was NOT created.",
                created.Error.Code, created.Error.Description);
            return;
        }

        var account = created.Value;
        account.AssignRole(userRole.Id);
        account.AssignRole(superAdminRole.Id);

        await dbContext.Accounts.AddAsync(account);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created the first administrator '{Username}' with the SuperAdmin role.", username);
    }
}
