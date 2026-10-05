using OmniCore.Shared.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------
// Register All Layers
// -------------------------------------------------------------

builder.Services.AddHealthChecks();
var app = builder.Build();

// -------------------------------------------------------------
// Middleware Pipeline
// -------------------------------------------------------------
// Automatically checks 'EnableSwagger' env/config or falls back to IsDevelopment()
app.UseApi(apiTitle: "OmniCore Auth API"); 
app.MapHealthChecks("/health"); 


app.Run();