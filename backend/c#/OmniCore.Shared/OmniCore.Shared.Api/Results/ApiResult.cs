namespace OmniCore.Shared.Api.Results;

public class ApiResult
{
    private const string DefaultSuccessMessage = "Success";

    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    protected ApiResult() { }

    public static ApiResult Success(string? message = null) => new()
    {
        IsSuccess = true,
        Message = string.IsNullOrWhiteSpace(message) ? DefaultSuccessMessage : message
    };
}

public sealed class ApiResult<TData> : ApiResult
{
    public TData? Data { get; init; }

    private ApiResult() { }

    public static ApiResult<TData> Success(TData data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = string.IsNullOrWhiteSpace(message) ? "Success" : message
    };
}