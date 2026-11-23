namespace OmniCore.Services.Notification.Application.Features.Notifications.Queries.GetNotifications;

using FluentValidation;
using OmniCore.Services.Notification.Domain.Enums;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.Status).Must(s => s is null || Enum.TryParse<NotificationStatus>(s, true, out _))
            .WithMessage("Status must be Pending, Sent or Failed.");
        RuleFor(x => x.Channel).Must(c => c is null || Enum.TryParse<NotificationChannel>(c, true, out _))
            .WithMessage("Channel must be Email or Webhook.");
        RuleFor(x => x.Recipient).MaximumLength(256);
    }
}
