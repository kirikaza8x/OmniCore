namespace OmniCore.Shared.Infrastructure.Outbox;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

public sealed class OutboxOptions : ConfigBase
{
    public override string SectionName => "Outbox";

    [Range(1, 60)]
    public int IntervalInSeconds { get; set; } = 3;

    [Range(1, 500)]
    public int BatchSize { get; set; } = 50;

    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;
}