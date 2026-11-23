using OmniCore.Services.Notification.Api;
using OmniCore.Services.Notification.Application;
using OmniCore.Services.Notification.Infrastructure;
using OmniCore.Shared.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNotificationApplication()
    .AddNotificationInfrastructure(builder.Configuration)
    .AddNotificationApi(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseApi(apiTitle: "OmniCore Notification API");
app.MapHealthChecks("/health");
app.MapCarter();

await app.UseNotificationInfrastructureAsync();

app.Run();

// Lets WebApplicationFactory<Program> reach the entry point from the integration tests.
public partial class Program { }
