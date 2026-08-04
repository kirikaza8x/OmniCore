namespace OmniCore.Services.Auth.Infrastructure.Persistence.Contexts;

using OmniCore.Shared.Infrastructure.Persistence.Factories;

public class AuthDbContextFactory : DesignTimeDbContextFactoryBase<AuthDbContext>
{
    protected override string ApiProjectName => "OmniCore.Services.Auth.Api";
    protected override string SchemaName => AuthDbContext.SchemaName;
}