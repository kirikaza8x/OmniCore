using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RoleId(value))
            .HasColumnName("id");

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Name).IsUnique();
    }
}