using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal static class CatalogConfiguration
{
    public static void ConfigureName<TEntity>(OwnedNavigationBuilder<TEntity, CatalogName> name)
        where TEntity : class
    {
        name.Property(value => value.Value)
            .HasColumnName("name")
            .HasMaxLength(CatalogName.MaximumLength)
            .IsRequired();
        name.Property(value => value.Normalized)
            .HasColumnName("normalized_name")
            .HasMaxLength(CatalogName.MaximumLength)
            .IsRequired();
        name.HasIndex(value => value.Normalized).IsUnique();
    }
}
