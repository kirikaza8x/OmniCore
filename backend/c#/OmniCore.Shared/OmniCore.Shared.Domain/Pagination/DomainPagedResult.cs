namespace OmniCore.Shared.Domain.Pagination;

public sealed record DomainPagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int CurrentPageSize => Items.Count;
    public int CurrentStartIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int CurrentEndIndex => TotalCount == 0 ? 0 : CurrentStartIndex + CurrentPageSize - 1;
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    // Parameterless constructor for standard deserializers
    public DomainPagedResult() { }

    public DomainPagedResult(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static DomainPagedResult<T> Empty => new(
        Array.Empty<T>(),
        1,
        10,
        0);

    public static DomainPagedResult<T> Create(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return new DomainPagedResult<T>(items, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Maps the items of the current PagedResult to a new target type (e.g., Domain Entity to DTO).
    /// </summary>
    public DomainPagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList().AsReadOnly();
        return new DomainPagedResult<TResult>(mappedItems, PageNumber, PageSize, TotalCount);
    }
}