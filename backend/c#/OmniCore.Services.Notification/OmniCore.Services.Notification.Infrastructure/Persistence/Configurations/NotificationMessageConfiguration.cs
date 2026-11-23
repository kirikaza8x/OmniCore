namespace OmniCore.Services.Notification.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OmniCore.Services.Notification.Domain.Entities;
using OmniCore.Services.Notification.Domain.ValueObjects;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    private static readonly ValueConverter<NotificationMessageId, Guid> IdConverter =
        new(id => id.Value, value => NotificationMessageId.From(value));

    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("notification_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasConversion(IdConverter).HasColumnName("id").ValueGeneratedNever();

        // Enums are stored as text so the table stays readable and adding a value needs no migration.
        builder.Property(m => m.Channel).HasConversion<string>().HasColumnName("channel").HasMaxLength(20).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20).IsRequired();

        builder.Property(m => m.Recipient).HasColumnName("recipient").HasMaxLength(NotificationMessage.MaxRecipientLength).IsRequired();
        builder.Property(m => m.TemplateKey).HasColumnName("template_key").HasMaxLength(64).IsRequired();
        builder.Property(m => m.Subject).HasColumnName("subject").HasMaxLength(300);
        builder.Property(m => m.Attempts).HasColumnName("attempts");
        builder.Property(m => m.LastError).HasColumnName("last_error").HasMaxLength(NotificationMessage.MaxErrorLength);
        builder.Property(m => m.DeduplicationKey).HasColumnName("deduplication_key").HasMaxLength(NotificationMessage.MaxDeduplicationKeyLength);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.LastAttemptAt).HasColumnName("last_attempt_at");
        builder.Property(m => m.SentAt).HasColumnName("sent_at");

        // One message per key (PostgreSQL allows many NULLs, so "no key" is unconstrained).
        builder.HasIndex(m => m.DeduplicationKey).IsUnique();
        builder.HasIndex(m => m.CreatedAt);
        builder.HasIndex(m => new { m.Status, m.CreatedAt });
        builder.HasIndex(m => m.Recipient);
    }
}
