namespace OmniCore.Shared.Infrastructure.Middleware;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Middleware responsible for extracting or generating a unique device identifier (<c>X-Device-ID</c>) 
/// for every incoming HTTP request and attaching it to both request context and response headers.
/// </summary>
public sealed class DeviceIdMiddleware
{
    /// <summary>
    /// The HTTP header key used to transport the device ID.
    /// </summary>
    public const string HeaderName = "X-Device-ID";

    /// <summary>
    /// The key used to store the device ID inside <see cref="HttpContext.Items"/>.
    /// </summary>
    public const string ItemKey = "DeviceId";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate in the HTTP pipeline.</param>
    public DeviceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to process the incoming HTTP request.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var deviceId) 
            || string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = deviceId;
        }

        string deviceIdValue = deviceId.ToString();

        // Store in HttpContext.Items for downstream application access
        context.Items[ItemKey] = deviceIdValue;

        // Echo device ID back to client in response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers[HeaderName] = deviceIdValue;
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}