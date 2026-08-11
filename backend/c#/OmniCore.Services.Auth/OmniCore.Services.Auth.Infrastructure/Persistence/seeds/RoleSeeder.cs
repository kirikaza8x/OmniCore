namespace OmniCore.Services.Auth.Infrastructure.Persistence.Seeds;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;
using OmniCore.Shared.Infrastructure.Data.Seeds;

public sealed class RoleSeeder(
    AuthDbContext dbContext,
    ILogger<RoleSeeder> logger) : IDataSeeder<Role>
{
    public int Order => 1;

    public async Task SeedAllAsync()
    {
        var defaultRoles = new (string Name, string Description)[]
        {
            ("User", "Standard user with basic access permissions."),
            ("Admin", "Administrator with elevated management permissions."),
            ("SuperAdmin", "Super administrator with full system permissions.")
        };

        var rolesToSeed = new List<Role>();

        foreach (var (name, description) in defaultRoles)
        {
            var exists = await dbContext.Roles.AnyAsync(r => r.Name == name);
            if (!exists)
            {
                var roleResult = Role.Create(name, description);
                if (roleResult.IsSuccess)
                {
                    rolesToSeed.Add(roleResult.Value);
                }
            }
        }

        if (rolesToSeed.Count != 0)
        {
            logger.LogInformation("Seeding {Count} default roles into database...", rolesToSeed.Count);
            await dbContext.Roles.AddRangeAsync(rolesToSeed);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Successfully seeded default roles.");
        }
        else
        {
            logger.LogInformation("All default roles already exist in database. Skipping seed.");
        }
    }
}