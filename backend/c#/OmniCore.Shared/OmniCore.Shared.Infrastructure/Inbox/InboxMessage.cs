namespace OmniCore.Shared.Infrastructure.Inbox;

using System;

/// <summary>
/// Represents an incoming integration event recorded in the Inbox table for idempotent processing.
/// </summary>
public sealed class InboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTime OccurredOnUtc { get; init; }

    public DateTime? ProcessedOnUtc { get; set; }

    public string? Error { get; set; }

    public int RetryCount { get; set; }
}