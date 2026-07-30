using OmniCore.Shared.Infrastructure.Configs;

namespace OmniCore.Shared.Infrastructure.Inbox;

public sealed class InboxOptions : ConfigBase
{
    public int IntervalInSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int RetentionDays { get; set; } = 7;

    public int MaxRetryCount { get; set; } = 3;
}