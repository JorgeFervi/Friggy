using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlanTemplates;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class DailyPlanTemplateMealConfiguration : IEntityTypeConfiguration<DailyPlanTemplateMeal>
{
    public void Configure(EntityTypeBuilder<DailyPlanTemplateMeal> builder)
    {
        builder.ToTable("daily_plan_template_meals", table =>
        {
            table.HasCheckConstraint("ck_daily_plan_template_meals_servings_positive", "servings > 0");
            table.HasCheckConstraint("ck_daily_plan_template_meals_order_non_negative", "\"order\" >= 0");
        });
        builder.HasKey(meal => meal.Id);
        builder.Property(meal => meal.Id).ValueGeneratedNever();
        builder.Property(meal => meal.DailyPlanTemplateId).HasColumnName("daily_plan_template_id");
        builder.Property(meal => meal.MealTypeId).HasColumnName("meal_type_id");
        builder.Property(meal => meal.RecipeId).HasColumnName("recipe_id");
        builder.Property(meal => meal.Servings).HasColumnName("servings");
        builder.Property(meal => meal.PlannedTime).HasColumnName("planned_time").HasColumnType("time without time zone");
        builder.Property(meal => meal.Order).HasColumnName("order");
        builder.HasAlternateKey(meal => new { meal.DailyPlanTemplateId, meal.MealTypeId });
        builder.HasIndex(meal => new { meal.DailyPlanTemplateId, meal.Order }).IsUnique();
        builder.HasOne<DailyPlanTemplate>().WithMany(planTemplate => planTemplate.Meals)
            .HasForeignKey(meal => meal.DailyPlanTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MealType>().WithMany().HasForeignKey(meal => meal.MealTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(meal => meal.RecipeId).OnDelete(DeleteBehavior.Restrict);
    }
}
