namespace OmniCore.Shared.Infrastructure.Services.SignalR;

using Microsoft.AspNetCore.SignalR;
using OmniCore.Shared.Application.Abstractions.SignalR;
using OmniCore.Shared.Application.DTOs;
using OmniCore.Shared.Infrastructure.Hubs;

/// <summary>
/// Real-time notifier that broadcasts log entries to connected SignalR hub clients.
/// </summary>
public sealed class SignalRLogNotifier : ILogNotifier
{
    private readonly IHubContext<LogHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRLogNotifier"/> class.
    /// </summary>
    public SignalRLogNotifier(IHubContext<LogHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task NotifyAsync(
        string category, 
        string message, 
        string level, 
        CancellationToken cancellationToken = default)
    {
        var payload = new LogNotificationDto(
            Category: category,
            Message: message,
            Level: level,
            TimestampUtc: DateTime.UtcNow);

        await _hubContext.Clients.All.SendAsync("ReceiveLog", payload, cancellationToken);
    }
}