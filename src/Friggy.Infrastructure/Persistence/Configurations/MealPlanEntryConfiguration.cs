using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de las asignaciones de comidas del plan semanal.
/// </summary>
internal sealed class MealPlanEntryConfiguration
    : IEntityTypeConfiguration<MealPlanEntry>
{
    /// <summary>
    /// Configura estados, restricciones, claves y relaciones de las asignaciones.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("meal_plan_entries", table =>
        {
            table.HasCheckConstraint(
                "ck_meal_plan_entries_servings_positive",
                "servings > 0");
            table.HasCheckConstraint(
                "ck_meal_plan_entries_status_valid",
                "status IN (0, 1, 2)");
            table.HasCheckConstraint(
                "ck_meal_plan_entries_completion_state",
                "(status = 1 AND completed_at IS NOT NULL) OR " +
                "(status <> 1 AND completed_at IS NULL)");
            table.HasCheckConstraint(
                "ck_meal_plan_entries_skipped_state",
                "(status = 2 AND skipped_reason IS NOT NULL AND btrim(skipped_reason) <> '') OR " +
                "(status <> 2 AND skipped_reason IS NULL AND alternative_description IS NULL)");
        });
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
        builder.Property(entry => entry.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .HasDefaultValue(MealPlanEntryStatus.Planned);
        builder.Property(entry => entry.CompletedAt)
            .HasColumnName("completed_at")
            .IsConcurrencyToken();
        builder.Property(entry => entry.SkippedReason).HasColumnName("skipped_reason");
        builder.Property(entry => entry.AlternativeDescription)
            .HasColumnName("alternative_description");
        builder.Ignore(entry => entry.IsCompleted);
        builder.Ignore(entry => entry.IsSkipped);
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
        builder.HasOne<MealPlanSlot>()
            .WithOne()
            .HasPrincipalKey<MealPlanSlot>(slot => new
            {
                slot.WeeklyPlanId,
                slot.Date,
                slot.MealTypeId,
            })
            .HasForeignKey<MealPlanEntry>(entry => new
            {
                entry.WeeklyPlanId,
                entry.Date,
                entry.MealTypeId,
            })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_meal_plan_entries_meal_plan_slots");
    }
}
