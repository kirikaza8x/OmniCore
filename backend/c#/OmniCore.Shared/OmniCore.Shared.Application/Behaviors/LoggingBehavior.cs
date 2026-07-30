namespace OmniCore.Shared.Application.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Pipeline behavior for structured request logging and OpenTelemetry activity tracking.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        string moduleName = GetModuleName(typeof(TRequest).FullName ?? requestName);

        // OpenTelemetry Activity tags
        Activity.Current?.SetTag("request.module", moduleName);
        Activity.Current?.SetTag("request.name", requestName);

        var stopwatch = Stopwatch.StartNew();

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Module"] = moduleName,
            ["RequestName"] = requestName
        }))
        {
            logger.LogInformation("Processing request {RequestName}", requestName);

            TResponse result = await next();

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Completed request {RequestName} successfully in {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarning(
                    "Completed request {RequestName} with error {@Error} in {ElapsedMilliseconds}ms",
                    requestName,
                    result.Error,
                    stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
    }

    private static string GetModuleName(string fullName)
    {
        string[] parts = fullName.Split('.');

        if (parts.Length > 1 && parts[0].Equals("OmniCore", StringComparison.OrdinalIgnoreCase))
        {
            return parts[1]; // e.g., OmniCore.Ordering -> Ordering
        }

        return parts.Length > 0 ? parts[0] : "Unknown";
    }
}