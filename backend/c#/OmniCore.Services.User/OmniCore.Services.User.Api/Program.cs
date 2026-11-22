using OmniCore.Services.User.Api;
using OmniCore.Services.User.Application;
using OmniCore.Services.User.Infrastructure;
using OmniCore.Shared.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddUserApplication()
    .AddUserInfrastructure(builder.Configuration)
    .AddUserApi(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

// Swagger is on when "EnableSwagger" is true, or by default in Development.
app.UseApi(apiTitle: "OmniCore User API");
app.MapHealthChecks("/health");
app.MapCarter();
await app.UseUserInfrastructureAsync();

app.Run();

// Lets the integration tests create the app with WebApplicationFactory<Program>.
public partial class Program { }
