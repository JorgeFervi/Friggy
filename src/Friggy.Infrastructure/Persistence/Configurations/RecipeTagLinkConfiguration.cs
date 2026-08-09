using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class RecipeTagLinkConfiguration
    : IEntityTypeConfiguration<RecipeTagLink>
{
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
