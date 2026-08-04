namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.ValueObjects;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("id");

        builder.Property(a => a.Username)
            .HasConversion(
                username => username.Value,
                value => Username.Create(value).Value)
            .HasColumnName("username")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(a => a.Username).IsUnique();

        builder.Property(a => a.Email)
            .HasConversion(
                email => email != null ? email.Value : null,
                value => !string.IsNullOrEmpty(value) ? EmailAddress.Create(value).Value : null)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired(false);

        builder.HasIndex(a => a.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        builder.Property(a => a.PasswordHash)
            .HasConversion(
                hash => hash != null ? hash.Value : null,
                value => value != null ? PasswordHash.Create(value).Value : null)
            .HasColumnName("password_hash")
            .HasMaxLength(500);

        builder.Property(a => a.IsEmailConfirmed)
            .HasColumnName("is_email_confirmed")
            .HasDefaultValue(false);

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(a => a.ModifiedAt).HasColumnName("modified_at");
        builder.Property(a => a.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);

        builder.HasQueryFilter(a => a.IsActive);
    }
}