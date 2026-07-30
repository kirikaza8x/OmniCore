namespace OmniCore.Shared.Domain.Queries;

public record Sort(string Field, SortOrder Order = SortOrder.Ascending);