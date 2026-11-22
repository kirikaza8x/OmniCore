namespace OmniCore.Services.User.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.User.Domain.Entities;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasConversion(Converters.UserSettingsIdConverter).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.UserId).HasConversion(Converters.UserIdConverter).HasColumnName("user_id").IsRequired();
        builder.HasIndex(s => s.UserId).IsUnique();

        builder.Property(s => s.Theme).HasColumnName("theme").HasMaxLength(20).IsRequired();
        builder.Property(s => s.Language).HasColumnName("language").HasMaxLength(10).IsRequired();
        builder.Property(s => s.EmailNotificationsEnabled).HasColumnName("email_notifications_enabled");
        builder.Property(s => s.PushNotificationsEnabled).HasColumnName("push_notifications_enabled");
    }
}
