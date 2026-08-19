using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la entidad RecipeTag.
/// </summary>
internal sealed class RecipeTagConfiguration : IEntityTypeConfiguration<RecipeTag>
{
    /// <summary>
    /// Configura la tabla y el nombre de las etiquetas de receta.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<RecipeTag> builder)
    {
        builder.ToTable("recipe_tags");
        builder.HasKey(item => item.Id);
        builder.OwnsOne(item => item.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(item => item.Name).IsRequired();
    }
}
