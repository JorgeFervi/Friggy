using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de la asociación entre pasos y líneas de
/// ingrediente.
/// </summary>
internal sealed class RecipeStepIngredientLinkConfiguration
    : IEntityTypeConfiguration<RecipeStepIngredientLink>
{
    /// <summary>
    /// Configura la tabla, la clave compuesta y las relaciones de la asociación.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<RecipeStepIngredientLink> builder)
    {
        builder.ToTable("recipe_step_ingredients");
        builder.HasKey(item => new { item.RecipeStepId, item.RecipeIngredientId });
        builder.Property(item => item.RecipeId).HasColumnName("recipe_id");
        builder.Property(item => item.RecipeStepId).HasColumnName("recipe_step_id");
        builder.Property(item => item.RecipeIngredientId).HasColumnName("recipe_ingredient_id");
        builder.HasOne<RecipeStep>()
            .WithMany(step => step.IngredientLinks)
            .HasForeignKey(item => new { item.RecipeId, item.RecipeStepId })
            .HasPrincipalKey(step => new { step.RecipeId, step.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RecipeIngredient>()
            .WithMany()
            .HasForeignKey(item => new { item.RecipeId, item.RecipeIngredientId })
            .HasPrincipalKey(ingredient => new { ingredient.RecipeId, ingredient.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
