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

        // Only the SHA-256 hex digest is stored (64 characters), see ADR 0003. The old plain `token` and
        // `replaced_by_token` columns still exist in the database until a later migration drops them.
        builder.Property(r => r.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(r => r.TokenHash).IsUnique();

        // Postgres' own row version (system column xmin) as an optimistic concurrency token (ADR 0003 step 2, A7).
        // Two parallel refreshes with the same token both try to revoke the same row; the second UPDATE finds the row
        // changed, affects nothing and throws DbUpdateConcurrencyException, so only one of them can ever succeed.
        // No schema change: xmin exists on every Postgres table.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(r => r.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(r => r.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        builder.Property(r => r.ReplacedByTokenHash)
            .HasColumnName("replaced_by_token_hash")
            .HasMaxLength(64);

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasOne(r => r.Account)
            .WithMany(a => a.RefreshTokens)
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}