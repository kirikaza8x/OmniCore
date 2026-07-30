namespace OmniCore.Shared.Infrastructure.Services.Time;

using OmniCore.Shared.Application.Abstractions.Time;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}