namespace OmniCore.Shared.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.Shared.Domain.DDD;


public static class AuditConfigurationExtensions
{
    public static EntityTypeBuilder<TEntity> ConfigureAuditProperties<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity,ISoftDeletable
    {
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256);

        builder.Property(x => x.ModifiedAt);

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(256);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        return builder;
    }
}