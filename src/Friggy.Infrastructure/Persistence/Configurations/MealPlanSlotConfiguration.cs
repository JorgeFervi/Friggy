using Friggy.Domain.Catalogs;
using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanSlotConfiguration
    : IEntityTypeConfiguration<MealPlanSlot>
{
    public void Configure(EntityTypeBuilder<MealPlanSlot> builder)
    {
        builder.ToTable(
            "meal_plan_slots",
            table => table.HasCheckConstraint(
                "ck_meal_plan_slots_order_non_negative",
                "\"order\" >= 0"));
        builder.HasKey(slot => slot.Id);
        builder.Property(slot => slot.Id).ValueGeneratedNever();
        builder.Property(slot => slot.WeeklyPlanId)
            .HasColumnName("weekly_plan_id");
        builder.Property(slot => slot.Date)
            .HasColumnName("date")
            .HasColumnType("date");
        builder.Property(slot => slot.MealTypeId)
            .HasColumnName("meal_type_id");
        builder.Property(slot => slot.Order)
            .HasColumnName("order");
        builder.HasAlternateKey(slot => new
        {
            slot.WeeklyPlanId,
            slot.Date,
            slot.MealTypeId,
        });
        builder.HasIndex(slot => new
        {
            slot.WeeklyPlanId,
            slot.Date,
            slot.Order,
        })
            .IsUnique();
        builder.HasOne<WeeklyPlan>()
            .WithMany(plan => plan.Slots)
            .HasForeignKey(slot => slot.WeeklyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MealType>()
            .WithMany()
            .HasForeignKey(slot => slot.MealTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
