using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new ExternalLoginId(value))
            .HasColumnName("id");

        builder.Property(e => e.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        // Provider + ProviderKey combination must be unique
        builder.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();

        builder.HasOne(e => e.Account)
            .WithMany(a => a.ExternalLogins)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}