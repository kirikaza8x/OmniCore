namespace OmniCore.Shared.Application.Abstractions.Tracing;

/// <summary>
/// Provides access to the current transaction's correlation ID across execution contexts.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the active Correlation ID for the current context.
    /// </summary>
    string CorrelationId { get; }
}