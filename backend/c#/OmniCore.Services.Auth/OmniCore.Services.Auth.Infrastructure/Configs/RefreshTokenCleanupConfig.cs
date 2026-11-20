namespace OmniCore.Services.Auth.Infrastructure.Configs;

using System.ComponentModel.DataAnnotations;
using OmniCore.Shared.Infrastructure.Configs;

/// <summary>Settings of the job that deletes dead refresh-token rows (ADR 0003, A10).</summary>
public class RefreshTokenCleanupConfig : ConfigBase
{
    public override string SectionName => "RefreshTokenCleanup";

    /// <summary>Rows are deleted once they have been expired for this many days. Revoked rows go the same way, so a
    /// rotated token stays around long enough for reuse detection until it would have expired anyway.</summary>
    [Range(1, 365)]
    public int RetentionDays { get; init; } = 30;

    [Range(1, 168)]
    public int IntervalHours { get; init; } = 6;

    public bool Enabled { get; init; } = true;
}
