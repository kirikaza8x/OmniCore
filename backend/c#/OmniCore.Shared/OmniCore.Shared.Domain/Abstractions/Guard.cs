namespace OmniCore.Shared.Domain.Abstractions;

public static class Guard
{
    public static T AgainstNull<T>(T? value, string paramName, string? message = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName, message ?? $"{paramName} cannot be null.");
        }

        return value;
    }

    public static string AgainstNullOrEmpty(string? value, string paramName, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message ?? $"{paramName} cannot be null or empty.", paramName);
        }

        return value;
    }

    public static T AgainstNegative<T>(T value, string paramName, string? message = null) where T : IComparable<T>
    {
        if (value.CompareTo(default!) < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message ?? $"{paramName} cannot be negative.");
        }

        return value;
    }
}