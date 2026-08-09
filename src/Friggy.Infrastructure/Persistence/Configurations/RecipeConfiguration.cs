using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
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
