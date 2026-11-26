namespace OmniCore.Shared.Api.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services, 
        string title = "OmniCore API", 
        string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = $"{title} Documentation"
            });

            // JWT Bearer Authentication Definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app, 
        string title = "OmniCore API",
        string version = "v1")
    {
        // Ensures Swashbuckle respects X-Forwarded-* headers from nginx,
        // so generated server URLs / "Try it out" calls match the public
        // gateway address instead of the internal container address.
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // Emit OpenAPI 3.0: the gateway's Swagger UI (pinned v5.17.x) only recognises 3.0.x and 3.1.x and otherwise says
        // "does not specify a valid version field" (issue #8). 3.0 is understood by every UI and client generator.
        app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0);
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"{version}/swagger.json", title); 
            options.RoutePrefix = "swagger";
            options.DefaultModelsExpandDepth(-1);
        });

        return app;
    }
}