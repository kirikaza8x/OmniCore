namespace OmniCore.Shared.Infrastructure.Inbox;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

/// <summary>
/// Configuration options for the Inbox pattern processor.
/// </summary>
public sealed class InboxOptions : ConfigBase
{
    public override string SectionName => "Inbox";

    [Range(1, 60)]
    public int IntervalInSeconds { get; set; } = 5;

    [Range(1, 500)]
    public int BatchSize { get; set; } = 20;

    [Range(1, 365)]
    public int RetentionDays { get; set; } = 7;

    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;
}