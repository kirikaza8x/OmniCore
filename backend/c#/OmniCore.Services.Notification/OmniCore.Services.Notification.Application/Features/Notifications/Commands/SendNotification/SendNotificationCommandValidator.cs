namespace OmniCore.Services.Notification.Application.Features.Notifications.Commands.SendNotification;

using FluentValidation;

public sealed class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(256);
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DeduplicationKey).MaximumLength(128);
        RuleFor(x => x.Model)
            .Must(m => m is null || m.Count <= 50).WithMessage("The model can have at most 50 entries.")
            .Must(m => m is null || m.Values.All(v => v is null || v.Length <= 2000)).WithMessage("Model values are limited to 2000 characters.");
    }
}
