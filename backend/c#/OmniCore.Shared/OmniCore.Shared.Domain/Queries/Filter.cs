namespace OmniCore.Shared.Domain.Queries;

public record Filter
{
    public string? Field { get; init; }
    public string? Operator { get; init; }
    public object? Value { get; init; }
    public string? Logic { get; init; } // "and" or "or"
    public IReadOnlyList<Filter>? Filters { get; init; }
}