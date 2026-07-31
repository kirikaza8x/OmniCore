namespace OmniCore.Shared.Infrastructure.Tracing;

using System.Threading;
using OmniCore.Shared.Application.Abstractions.Tracing;

/// <summary>
/// Manages the ambient correlation ID state using <see cref="AsyncLocal{T}"/>.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    /// <inheritdoc />
    public string CorrelationId
    {
        get => CurrentCorrelationId.Value ?? string.Empty;
        set => CurrentCorrelationId.Value = value;
    }
}