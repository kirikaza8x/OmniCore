namespace OmniCore.Services.User.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.User.Domain.Entities;

public class UserTenantProfileConfiguration : IEntityTypeConfiguration<UserTenantProfile>
{
    public void Configure(EntityTypeBuilder<UserTenantProfile> builder)
    {
        builder.ToTable("tenant_profiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(Converters.UserTenantProfileIdConverter)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // The tenant column is stamped by TenantSaveChangesInterceptor and filtered by ApplyTenantQueryFilters.
        builder.Property(p => p.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(p => p.UserId)
            .HasConversion(Converters.UserIdConverter)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(p => p.GamerTag)
            .HasColumnName("gamer_tag")
            .HasMaxLength(UserTenantProfile.MaxGamerTagLength)
            .IsRequired();

        builder.Property(p => p.CustomAvatarUrl)
            .HasColumnName("custom_avatar_url")
            .HasMaxLength(UserTenantProfile.MaxAvatarUrlLength);

        builder.Property(p => p.JoinedAtUtc).HasColumnName("joined_at_utc");

        // One profile per person per tenant.
        builder.HasIndex(p => new { p.UserId, p.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_tenant_profiles_user_id_tenant_id");

        // No navigation: a tenant profile is read through its own repository, never through UserProfile.
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_tenant_profiles_user_profiles_user_id");
    }
}
