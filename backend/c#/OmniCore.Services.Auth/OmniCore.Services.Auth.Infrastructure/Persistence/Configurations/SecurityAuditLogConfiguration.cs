using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("security_audit_logs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new SecurityAuditLogId(value))
            .HasColumnName("id");

        builder.Property(s => s.AccountId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? new AccountId(value.Value) : null)
            .HasColumnName("account_id")
            .IsRequired(false);

        builder.Property(s => s.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        // IpAddress Value Object Conversion
        builder.Property(s => s.IpAddress)
            .HasConversion(
                ip => ip.Value,
                value => IpAddress.Create(value).Value)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired();

        // UserAgent Value Object Conversion
        builder.Property(s => s.UserAgent)
            .HasConversion(
                ua => ua.Value,
                value => UserAgent.Create(value).Value)
            .HasColumnName("user_agent")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.MetadataJson)
            .HasColumnName("metadata_json");

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasOne(s => s.Account)
            .WithMany(a => a.SecurityAuditLogs)
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}