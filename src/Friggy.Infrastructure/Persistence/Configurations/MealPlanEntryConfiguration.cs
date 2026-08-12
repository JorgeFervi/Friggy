using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanEntryConfiguration
    : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable(
            "meal_plan_entries",
            table => table.HasCheckConstraint(
                "ck_meal_plan_entries_servings_positive",
                "servings > 0"));
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.WeeklyPlanId).HasColumnName("weekly_plan_id");
        builder.Property(entry => entry.Date)
            .HasColumnName("date")
            .HasColumnType("date");
        builder.Property(entry => entry.MealTypeId).HasColumnName("meal_type_id");
        builder.Property(entry => entry.RecipeId).HasColumnName("recipe_id");
        builder.Property(entry => entry.Servings)
            .HasColumnName("servings")
            .HasDefaultValue(1);
        builder.Property(entry => entry.CompletedAt)
            .HasColumnName("completed_at")
            .IsConcurrencyToken();
        builder.Ignore(entry => entry.IsCompleted);
        builder.HasIndex(entry => new
        {
            entry.WeeklyPlanId,
            entry.Date,
            entry.MealTypeId,
        })
            .IsUnique();
        builder.HasOne<WeeklyPlan>()
            .WithMany(plan => plan.Entries)
            .HasForeignKey(entry => entry.WeeklyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(entry => entry.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(entry => entry.MealTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
