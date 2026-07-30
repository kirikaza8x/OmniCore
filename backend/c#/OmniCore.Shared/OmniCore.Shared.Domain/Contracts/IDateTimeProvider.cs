namespace OmniCore.Shared.Domain.Contracts;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}