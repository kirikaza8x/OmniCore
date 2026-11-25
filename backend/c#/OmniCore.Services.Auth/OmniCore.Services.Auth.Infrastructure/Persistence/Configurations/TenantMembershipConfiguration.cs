namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Infrastructure.Data.Tenancy;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");

        builder.HasKey(m => new { m.TenantId, m.AccountId });

        builder.Property(m => m.TenantId)
            .HasConversion<TenantIdConverter>()
            .HasColumnName("tenant_id");

        builder.Property(m => m.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id");

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.JoinedAtUtc)
            .HasColumnName("joined_at_utc");

        builder.HasIndex(m => m.AccountId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(m => m.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
