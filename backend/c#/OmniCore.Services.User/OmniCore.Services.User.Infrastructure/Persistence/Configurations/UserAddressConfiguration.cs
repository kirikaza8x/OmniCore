namespace OmniCore.Services.User.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.User.Domain.Entities;

public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("user_addresses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasConversion(Converters.UserAddressIdConverter).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.UserId).HasConversion(Converters.UserIdConverter).HasColumnName("user_id").IsRequired();
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.AddressLine1).HasColumnName("address_line1").HasMaxLength(150).IsRequired();
        builder.Property(a => a.AddressLine2).HasColumnName("address_line2").HasMaxLength(150);
        builder.Property(a => a.City).HasColumnName("city").HasMaxLength(50).IsRequired();
        builder.Property(a => a.State).HasColumnName("state").HasMaxLength(50).IsRequired();
        builder.Property(a => a.Country).HasColumnName("country").HasMaxLength(50).IsRequired();
        builder.Property(a => a.ZipCode).HasColumnName("zip_code").HasMaxLength(20).IsRequired();
        builder.Property(a => a.IsPrimary).HasColumnName("is_primary");
    }
}
