namespace OmniCore.Services.User.Infrastructure.Persistence.Contexts;

using OmniCore.Shared.Infrastructure.Persistence.Factories;

/// <summary>Used by <c>dotnet ef</c> (design time) to build the context without starting the API.</summary>
public class UserDbContextFactory : DesignTimeDbContextFactoryBase<UserDbContext>
{
    protected override string ApiProjectName => "OmniCore.Services.User.Api";
    protected override string SchemaName => UserDbContext.SchemaName;
}
