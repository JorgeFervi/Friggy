using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable(
            "recipe_steps",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_recipe_steps_estimated_time_non_negative",
                    "estimated_time IS NULL OR estimated_time >= interval '0 minutes'");
                table.HasCheckConstraint(
                    "ck_recipe_steps_order_non_negative",
                    "\"order\" >= 0");
            });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.RecipeId, item.Id });
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.RecipeId).HasColumnName("recipe_id");
        builder.Property(item => item.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();
        builder.Property(item => item.EstimatedTime).HasColumnName("estimated_time");
        builder.Property(item => item.Order).HasColumnName("order");
        builder.HasIndex(item => new { item.RecipeId, item.Order }).IsUnique();
        builder.Ignore(item => item.RecipeIngredientIds);
        builder.Navigation(item => item.IngredientLinks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<Recipe>()
            .WithMany(recipe => recipe.Steps)
            .HasForeignKey(item => item.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
