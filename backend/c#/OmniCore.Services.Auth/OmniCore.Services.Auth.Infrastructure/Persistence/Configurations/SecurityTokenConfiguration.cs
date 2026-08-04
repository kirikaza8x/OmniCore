using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class SecurityTokenConfiguration : IEntityTypeConfiguration<SecurityToken>
{
    public void Configure(EntityTypeBuilder<SecurityToken> builder)
    {
        builder.ToTable("security_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new SecurityTokenId(value))
            .HasColumnName("id");

        builder.Property(t => t.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(t => t.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(t => t.CodeHash);

        builder.Property(t => t.TokenType)
            .HasConversion<int>()
            .HasColumnName("token_type")
            .IsRequired();

        builder.Property(t => t.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(t => t.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasOne(t => t.Account)
            .WithMany(a => a.SecurityTokens)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}