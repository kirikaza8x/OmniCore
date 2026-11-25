namespace OmniCore.Shared.Api.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

/// <summary>
/// Logs, traces and metrics through OpenTelemetry, exported over OTLP (ADR 0011). Nothing happens unless an
/// <c>OTEL_EXPORTER_OTLP_*ENDPOINT</c> variable is set, so tests and plain local runs are unchanged. The service name
/// comes from <c>OTEL_SERVICE_NAME</c>; the per-signal endpoints and the protocol from the standard <c>OTEL_EXPORTER_OTLP_*</c> variables.
/// No SQL text, request bodies or headers are recorded.
/// </summary>
public static class ObservabilityExtensions
{
    private static readonly string[] EndpointKeys =
    [
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT",
        "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
        "OTEL_EXPORTER_OTLP_METRICS_ENDPOINT",
    ];

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        if (!EndpointKeys.Any(key => !string.IsNullOrWhiteSpace(configuration[key])))
        {
            return services;
        }

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.AddOtlpExporter();
        }));

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return services;
    }
}
