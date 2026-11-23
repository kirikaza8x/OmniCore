namespace OmniCore.Services.Notification.Infrastructure.Persistence.Contexts;

using OmniCore.Shared.Infrastructure.Persistence.Factories;

/// <summary>Used by <c>dotnet ef</c> (design time) to build the context without starting the API.</summary>
public class NotificationDbContextFactory : DesignTimeDbContextFactoryBase<NotificationDbContext>
{
    protected override string ApiProjectName => "OmniCore.Services.Notification.Api";
    protected override string SchemaName => NotificationDbContext.SchemaName;
}
