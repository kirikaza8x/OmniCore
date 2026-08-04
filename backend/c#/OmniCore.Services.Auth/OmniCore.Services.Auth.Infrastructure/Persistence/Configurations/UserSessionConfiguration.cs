namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new UserSessionId(value))
            .HasColumnName("id");

        builder.Property(s => s.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(s => s.RefreshTokenId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? new RefreshTokenId(value.Value) : null)
            .HasColumnName("refresh_token_id")
            .IsRequired(false);

        builder.Property(s => s.DeviceName)
            .HasColumnName("device_name")
            .HasMaxLength(200);

        builder.Property(s => s.IpAddress)
            .HasConversion(
                ip => ip.Value,
                value => IpAddress.Create(value).Value)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(s => s.UserAgent)
            .HasConversion(
                ua => ua.Value,
                value => UserAgent.Create(value).Value)
            .HasColumnName("user_agent")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.LastActiveAtUtc)
            .HasColumnName("last_active_at_utc");

        builder.Property(s => s.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        // DB Foreign Key Constraint retained without C# object navigation properties
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.AccountId);

        builder.HasOne(s => s.RefreshToken)
            .WithOne(r => r.UserSession)
            .HasForeignKey<UserSession>(s => s.RefreshTokenId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}