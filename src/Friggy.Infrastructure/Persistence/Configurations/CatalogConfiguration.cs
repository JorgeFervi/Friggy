using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Clase auxiliar que configura el value object CatalogName para las entidades
/// que lo utilizan.
/// </summary>
internal static class CatalogConfiguration
{
    /// <summary>
    /// Configura las columnas, longitud e índice único del nombre de catálogo.
    /// </summary>
    /// <typeparam name="TEntity">
    /// Tipo de entidad que posee el nombre de catálogo.
    /// </typeparam>
    /// <param name="name">
    /// Constructor de configuración de la navegación del nombre.
    /// </param>
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
