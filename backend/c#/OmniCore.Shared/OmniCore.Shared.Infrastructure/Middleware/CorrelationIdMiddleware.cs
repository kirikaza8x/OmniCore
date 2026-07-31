namespace OmniCore.Shared.Infrastructure.Middleware;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OmniCore.Shared.Infrastructure.Tracing;

/// <summary>
/// HTTP middleware that extracts or generates a unique Correlation ID for each inbound request.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderKey = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CorrelationIdAccessor accessor)
    {
        string correlationId = context.Request.Headers[HeaderKey].FirstOrDefault() 
                               ?? Guid.NewGuid().ToString("N");

        accessor.CorrelationId = correlationId;
        context.Items[HeaderKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderKey))
            {
                context.Response.Headers.Append(HeaderKey, correlationId);
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}