namespace OmniCore.Shared.Domain.Queries;

public interface IPageable
{
    int PageNumber { get; }
    int PageSize { get; }
    int Skip { get; }
}