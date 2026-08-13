using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class RecipeIngredientConfiguration
    : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable(
            "recipe_ingredients",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_recipe_ingredients_quantity_positive",
                    "quantity > 0");
                table.HasCheckConstraint(
                    "ck_recipe_ingredients_order_non_negative",
                    "\"order\" >= 0");
            });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.RecipeId, item.Id });
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.RecipeId).HasColumnName("recipe_id");
        builder.Property(item => item.IngredientId).HasColumnName("ingredient_id");
        builder.Property(item => item.UnitTypeId).HasColumnName("unit_type_id");
        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(12, 3);
        builder.Property(item => item.Order).HasColumnName("order");
        builder.HasOne<Recipe>()
            .WithMany(recipe => recipe.Ingredients)
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Ingredient>()
            .WithMany()
            .HasForeignKey(item => item.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitType>()
            .WithMany()
            .HasForeignKey(item => item.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
