namespace OmniCore.Services.Notification.Domain.ValueObjects;

using OmniCore.Shared.Domain.ValueObject;

public sealed class NotificationMessageId : ValueObject
{
    public Guid Value { get; }

    private NotificationMessageId(Guid value)
    {
        Value = value;
    }

    public static NotificationMessageId From(Guid value) => new(value);
    public static NotificationMessageId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}
