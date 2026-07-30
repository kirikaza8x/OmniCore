namespace OmniCore.Shared.Domain.Queries;

public record PagedQuery : IPageable, ISortable
{
    private readonly int _pageNumber = 1;
    private readonly int _pageSize = 10;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }

    public string? SortColumn { get; init; } = "CreatedAt";
    public SortOrder SortOrder { get; init; } = SortOrder.Descending;

    public int Skip => (PageNumber - 1) * PageSize;
}