namespace OmniCore.Shared.Api.RateLimiting;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.20",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Quota exceeded. Please slow down your requests."
                }, cancellationToken: token);
            };

            // 1. Global Policy
            options.AddFixedWindowLimiter(RateLimitPolicies.Global, opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 10;
            });

            // 2. Auth Policy (Prevent brute force logins)
            options.AddFixedWindowLimiter(RateLimitPolicies.Auth, opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            // 3. AI Generation Policy
            options.AddTokenBucketLimiter(RateLimitPolicies.AiGenerate, opt =>
            {
                opt.TokenLimit = 5;
                opt.QueueLimit = 2;
                opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                opt.TokensPerPeriod = 2;
            });

            // 4. Payment Policy
            options.AddFixedWindowLimiter(RateLimitPolicies.Payment, opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
            });

            // 5. Order Policy
            options.AddFixedWindowLimiter(RateLimitPolicies.Order, opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromMinutes(1);
            });
        });

        return services;
    }
}