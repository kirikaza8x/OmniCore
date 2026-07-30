namespace OmniCore.Shared.Infrastructure.Logging;

using System.Threading.Channels;
using Microsoft.Extensions.Logging;

/// <summary>
/// Custom <see cref="ILogger"/> implementation that writes log entries into an in-memory channel for real-time SignalR streaming.
/// </summary>
public sealed class SignalRLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ChannelWriter<LogMessageEntry> _writer;
    private static readonly AsyncLocal<bool> IsLogging = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRLogger"/> class.
    /// </summary>
    public SignalRLogger(string categoryName, ChannelWriter<LogMessageEntry> writer)
    {
        _categoryName = categoryName;
        _writer = writer;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel < LogLevel.Information) 
            return false;

        // Prevent infinite recursive logging loops from ASP.NET Core & SignalR internals
        if (_categoryName.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
            _categoryName.StartsWith("System.Net", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        // Prevent thread reentrancy
        if (IsLogging.Value) return;

        try
        {
            IsLogging.Value = true;

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message)) return;

            var entry = new LogMessageEntry(_categoryName, message, logLevel.ToString());
            
            // Non-blocking write to channel
            _writer.TryWrite(entry);
        }
        finally
        {
            IsLogging.Value = false;
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}