using OmniCore.Services.Auth.Api;
using OmniCore.Services.Auth.Application;
using OmniCore.Services.Auth.Infrastructure;
using OmniCore.Shared.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------
// Register All Layers
// -------------------------------------------------------------
builder.Services
    .AddAuthApplication()
    .AddAuthInfrastructure(builder.Configuration)
    .AddAuthApi(builder.Configuration);
builder.Services.AddHealthChecks();
var app = builder.Build();

// -------------------------------------------------------------
// Middleware Pipeline
// -------------------------------------------------------------
// Automatically checks 'EnableSwagger' env/config or falls back to IsDevelopment()
app.UseApi(apiTitle: "OmniCore Auth API"); 
app.MapHealthChecks("/health"); 
app.MapCarter();
await app.UseAuthInfrastructureAsync();

app.Run();