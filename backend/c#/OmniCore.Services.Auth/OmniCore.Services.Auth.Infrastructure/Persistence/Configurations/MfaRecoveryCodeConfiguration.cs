using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class MfaRecoveryCodeConfiguration : IEntityTypeConfiguration<MfaRecoveryCode>
{
    public void Configure(EntityTypeBuilder<MfaRecoveryCode> builder)
    {
        builder.ToTable("mfa_recovery_codes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new MfaRecoveryCodeId(value))
            .HasColumnName("id");

        builder.Property(c => c.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(c => c.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasOne(c => c.Account)
            .WithMany(a => a.MfaRecoveryCodes)
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}