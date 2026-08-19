using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de las recetas y sus colecciones agregadas.
/// </summary>
internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    /// <summary>
    /// Configura la tabla, el tiempo estimado, el nombre y el acceso a las
    /// colecciones de la receta.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable(
            "recipes",
            table => table.HasCheckConstraint(
                "ck_recipes_estimated_time_non_negative",
                "estimated_time >= interval '0 minutes'"));
        builder.HasKey(item => item.Id);
        builder.OwnsOne(item => item.Name, CatalogConfiguration.ConfigureName);
        builder.Navigation(item => item.Name).IsRequired();
        builder.Property(item => item.EstimatedTime)
            .HasColumnName("estimated_time")
            .IsRequired();
        builder.Ignore(item => item.TagIds);
        builder.Ignore(item => item.MealTypeIds);
        builder.Navigation(item => item.Ingredients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Steps)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Tags)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.MealTypes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
