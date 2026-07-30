namespace OmniCore.Shared.Application.DTOs.Queries;

public record PagedRequest
{
    private const int MaxPageSize = 100;
    private readonly int _pageSize = 10;

    public int PageNumber { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : (value <= 0 ? 10 : value);
    }

    public string? SearchTerm { get; init; }
    public string SortColumn { get; init; } = "CreatedAt";
    public string SortDirection { get; init; } = "desc";

    public IEnumerable<FilterRequest>? Filters { get; init; }
}

public record FilterRequest(
    string Field,
    string Operator,
    string? Value,
    string? Logic = "and");

public record SortRequest(
    string Field,
    string Direction = "asc");