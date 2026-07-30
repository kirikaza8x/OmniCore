namespace OmniCore.Shared.Domain.ValueObjects;

using OmniCore.Shared.Domain.Abstractions;

public sealed record DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    private DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        if (end < start)
        {
            return Error.Validation("DateRange.Invalid", "End date cannot be prior to start date.");
        }

        return new DateRange(start, end);
    }

    public int DurationInDays => (End - Start).Days;

    public bool Overlaps(DateRange other) => Start < other.End && End > other.Start;

    public bool Contains(DateTime date) => date >= Start && date <= End;
}