using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la asociación entre recetas y tipos de comida.
/// </summary>
internal sealed class RecipeMealTypeLinkConfiguration
    : IEntityTypeConfiguration<RecipeMealTypeLink>
{
    /// <summary>
    /// Configura la tabla, la clave compuesta y las relaciones de la asociación.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<RecipeMealTypeLink> builder)
    {
        builder.ToTable("recipe_meal_types");
        builder.HasKey(item => new { item.RecipeId, item.MealTypeId });
        builder.Property(item => item.RecipeId).HasColumnName("recipe_id");
        builder.Property(item => item.MealTypeId).HasColumnName("meal_type_id");
        builder.HasOne<Recipe>()
            .WithMany(recipe => recipe.MealTypes)
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(item => item.MealTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
