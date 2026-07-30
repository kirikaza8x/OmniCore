namespace Microsoft.AspNetCore.Builder;

using Microsoft.AspNetCore.Http;
using OmniCore.Shared.Infrastructure.Middleware;

/// <summary>
/// Extension methods for registering and consuming the <see cref="DeviceIdMiddleware"/>.
/// </summary>
public static class DeviceIdMiddlewareExtensions
{
    /// <summary>
    /// Registers the <see cref="DeviceIdMiddleware"/> in the HTTP request processing pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseDeviceId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<DeviceIdMiddleware>();
    }

    /// <summary>
    /// Retrieves the device ID attached to the active <see cref="HttpContext"/> by the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The device ID string if available; otherwise, <c>null</c>.</returns>
    public static string? GetDeviceId(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items[DeviceIdMiddleware.ItemKey] as string;
    }
}