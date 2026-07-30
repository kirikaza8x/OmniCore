namespace OmniCore.Shared.Application.Abstractions.SignalR;

public interface ILogNotifier
{
    Task NotifyAsync(string category, string message, string level, CancellationToken cancellationToken = default);
}