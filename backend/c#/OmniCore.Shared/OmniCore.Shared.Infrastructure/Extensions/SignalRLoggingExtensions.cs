namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Application.Abstractions.SignalR;
using OmniCore.Shared.Infrastructure.Logging;
using OmniCore.Shared.Infrastructure.Services.SignalR;

/// <summary>
/// Extension methods for setting up SignalR real-time logging provider in the logging builder.
/// </summary>
public static class SignalRLoggingExtensions
{
    /// <summary>
    /// Adds a custom SignalR logger provider to the <see cref="ILoggingBuilder"/>.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    /// <returns>The updated <see cref="ILoggingBuilder"/>.</returns>
    public static ILoggingBuilder AddSignalRLogger(this ILoggingBuilder builder)
    {
        builder.Services.TryAddScoped<ILogNotifier, SignalRLogNotifier>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, SignalRLoggerProvider>());

        return builder;
    }
}