namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Shared.Infrastructure.Data.Tenancy;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion<TenantIdConverter>()
            .HasColumnName("id");

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .HasMaxLength(63)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active");

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}
