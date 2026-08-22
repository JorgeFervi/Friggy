using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>Configura los huecos de comida diarios.</summary>
internal sealed class DailyMealPlanSlotConfiguration
    : IEntityTypeConfiguration<MealPlanSlot>
{
    /// <summary>Configura orden, horario, claves y relaciones.</summary>
    public void Configure(EntityTypeBuilder<MealPlanSlot> builder)
    {
        builder.ToTable(
            "meal_plan_slots",
            table => table.HasCheckConstraint(
                "ck_meal_plan_slots_order_non_negative",
                "\"order\" >= 0"));
        builder.HasKey(slot => slot.Id);
        builder.Property(slot => slot.Id).ValueGeneratedNever();
        builder.Property(slot => slot.DailyPlanId).HasColumnName("daily_plan_id");
        builder.Property(slot => slot.MealTypeId).HasColumnName("meal_type_id");
        builder.Property(slot => slot.Order).HasColumnName("order");
        builder.Property(slot => slot.PlannedTime)
            .HasColumnName("planned_time")
            .HasColumnType("time without time zone");
        builder.HasAlternateKey(slot => new { slot.DailyPlanId, slot.MealTypeId });
        builder.HasIndex(slot => new { slot.DailyPlanId, slot.Order }).IsUnique();
        builder.HasOne<DailyPlan>()
            .WithMany(plan => plan.Slots)
            .HasForeignKey(slot => slot.DailyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(slot => slot.MealTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
