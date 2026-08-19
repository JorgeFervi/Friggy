using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la entidad Ingredient.
/// </summary>
internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    /// <summary>
    /// Configura la tabla, la clave y el nombre de los ingredientes.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");
        builder.HasKey(item => item.Id);
        builder.OwnsOne(item => item.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(item => item.Name).IsRequired();
    }
}
