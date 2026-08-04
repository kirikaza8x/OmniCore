using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Infrastructure.Persistence.Configurations;

public class AccountRoleConfiguration : IEntityTypeConfiguration<AccountRole>
{
    public void Configure(EntityTypeBuilder<AccountRole> builder)
    {
        builder.ToTable("account_roles");

        // Composite Primary Key
        builder.HasKey(ar => new { ar.AccountId, ar.RoleId });

        builder.Property(ar => ar.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .HasColumnName("account_id");

        builder.Property(ar => ar.RoleId)
            .HasConversion(id => id.Value, value => new RoleId(value))
            .HasColumnName("role_id");

        builder.Property(ar => ar.AssignedAtUtc)
            .HasColumnName("assigned_at_utc");

        // Foreign Key Relationships
        builder.HasOne(ar => ar.Account)
            .WithMany(a => a.AccountRoles)
            .HasForeignKey(ar => ar.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.Role)
            .WithMany(r => r.AccountRoles)
            .HasForeignKey(ar => ar.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}