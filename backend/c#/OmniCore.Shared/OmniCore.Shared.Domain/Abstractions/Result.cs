namespace OmniCore.Shared.Domain.Abstractions;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None ||
            !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error state for Result", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);

    // Implicit conversion from Error to Result
    public static implicit operator Result(Error error) => Failure(error);

    // Functional pattern matching helper
    public TMatch Match<TMatch>(Func<TMatch> onSuccess, Func<Error, TMatch> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public TValue? GetValueOrDefault() => _value;

    // Implicit conversions
    public static implicit operator Result<TValue>(TValue value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    // Functional pattern matching helper
    public TMatch Match<TMatch>(Func<TValue, TMatch> onSuccess, Func<Error, TMatch> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}