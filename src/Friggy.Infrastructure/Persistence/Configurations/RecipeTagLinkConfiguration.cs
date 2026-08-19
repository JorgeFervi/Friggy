using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la asociación entre recetas y etiquetas.
/// </summary>
internal sealed class RecipeTagLinkConfiguration
    : IEntityTypeConfiguration<RecipeTagLink>
{
    /// <summary>
    /// Configura la tabla, la clave compuesta y las relaciones de la asociación.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<RecipeTagLink> builder)
    {
        builder.ToTable("recipe_recipe_tags");
        builder.HasKey(item => new { item.RecipeId, item.RecipeTagId });
        builder.Property(item => item.RecipeId).HasColumnName("recipe_id");
        builder.Property(item => item.RecipeTagId).HasColumnName("recipe_tag_id");
        builder.HasOne<Recipe>()
            .WithMany(recipe => recipe.Tags)
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RecipeTag>()
            .WithMany()
            .HasForeignKey(item => item.RecipeTagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
