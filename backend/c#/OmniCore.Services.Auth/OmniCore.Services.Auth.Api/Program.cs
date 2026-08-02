using Carter;
using OmniCore.Shared.Api;
using OmniCore.Services.Auth.Api;
using OmniCore.Services.Auth.Application;

var builder = WebApplication.CreateBuilder(args);

// Register Shared API Kernel (Carter, Auth, Rate Limiting, CORS)
builder.Services.AddApi(
    new[]
    {
        ApiAssemblyReference.Assembly,
        ApplicationAssemblyReference.Assembly
    },
    builder.Configuration
);

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseApi();
app.MapCarter();

app.Run();
