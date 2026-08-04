using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class MfaMethodConfiguration : IEntityTypeConfiguration<MfaMethod>
{
    public void Configure(EntityTypeBuilder<MfaMethod> builder)
    {
        builder.ToTable("mfa_methods");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MfaMethodId(value))
            .HasColumnName("id");

        builder.Property(m => m.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(m => m.Type)
            .HasConversion<int>()
            .HasColumnName("type")
            .IsRequired();

        builder.Property(m => m.Secret)
            .HasColumnName("secret")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.IsVerified)
            .HasColumnName("is_verified")
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasOne(m => m.Account)
            .WithMany(a => a.MfaMethods)
            .HasForeignKey(m => m.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}