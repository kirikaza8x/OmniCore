using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RefreshTokenId(value))
            .HasColumnName("id");

        builder.Property(r => r.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(r => r.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(r => r.Token).IsUnique();

        builder.Property(r => r.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(r => r.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        builder.Property(r => r.ReplacedByToken)
            .HasColumnName("replaced_by_token")
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasOne(r => r.Account)
            .WithMany(a => a.RefreshTokens)
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}