namespace OmniCore.Shared.Infrastructure.Logging;

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Application.Abstractions.SignalR;

/// <summary>
/// Custom <see cref="ILoggerProvider"/> that streams application log messages to clients via SignalR.
/// </summary>
public sealed class SignalRLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, SignalRLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Channel<LogMessageEntry> _logChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRLoggerProvider"/> class.
    /// </summary>
    public SignalRLoggerProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        var channelOptions = new BoundedChannelOptions(2000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        _logChannel = Channel.CreateBounded<LogMessageEntry>(channelOptions);
        _processingTask = Task.Run(ProcessLogQueueAsync);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, category => new SignalRLogger(category, _logChannel.Writer));
    }

    private async Task ProcessLogQueueAsync()
    {
        try
        {
            while (await _logChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_logChannel.Reader.TryRead(out var entry))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var notifier = scope.ServiceProvider.GetService<ILogNotifier>();
                        if (notifier is not null)
                        {
                            await notifier.NotifyAsync(entry.Category, entry.Message, entry.LogLevel, _cts.Token);
                        }
                    }
                    catch
                    {
                        // Ignore background logging handler failures
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during provider disposal
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            // Wait up to 2 seconds for processing task to complete gracefully
            _processingTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Suppress background thread cancellation exceptions
        }

        _cts.Dispose();
        _loggers.Clear();
    }
}

/// <summary>
/// Immutable log message payload queued for SignalR broadcast.
/// </summary>
/// <param name="Category">The log category source name.</param>
/// <param name="Message">The formatted log message body.</param>
/// <param name="LogLevel">The severity level string.</param>
public record LogMessageEntry(string Category, string Message, string LogLevel);