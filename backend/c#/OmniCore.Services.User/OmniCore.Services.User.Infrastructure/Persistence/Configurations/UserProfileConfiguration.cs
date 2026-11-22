namespace OmniCore.Services.User.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.ValueObjects;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        // The id is the Auth account id: it is given, never generated here.
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(Converters.UserIdConverter)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Display copies of Auth's data (ADR 0004): deliberately NOT unique.
        builder.Property(p => p.Username)
            .HasConversion(u => u.Value, value => Username.Create(value).Value)
            .HasColumnName("username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasConversion(
                email => email != null ? email.Value : null,
                value => !string.IsNullOrEmpty(value) ? EmailAddress.Create(value).Value : null)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(UserProfile.MaxNameLength);
        builder.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(UserProfile.MaxNameLength);
        builder.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(2 * UserProfile.MaxNameLength + 1);
        builder.Property(p => p.Bio).HasColumnName("bio").HasMaxLength(UserProfile.MaxBioLength);
        builder.Property(p => p.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(2048);
        builder.Property(p => p.PhoneNumber).HasColumnName("phone_number").HasMaxLength(UserProfile.MaxPhoneLength);

        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAtUtc).HasColumnName("deleted_at_utc");

        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(p => p.ModifiedAt).HasColumnName("modified_at");
        builder.Property(p => p.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);

        builder.HasMany(p => p.Addresses)
            .WithOne(a => a.UserProfile)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(p => p.Settings)
            .WithOne(s => s.UserProfile)
            .HasForeignKey<UserSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tenant profiles are not stored until the owner decides on multi-tenancy (ADR 0001, ADR 0004).
        builder.Ignore(p => p.TenantProfiles);
    }
}
