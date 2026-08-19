using Friggy.Domain.Inventory;
using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de persistencia de los movimientos de inventario y sus
/// restricciones de cantidad.
/// </summary>
internal sealed class InventoryMovementConfiguration
    : IEntityTypeConfiguration<InventoryMovement>
{
    /// <summary>
    /// Configura la tabla, variaciones, cantidades resultantes y relaciones de
    /// los movimientos.
    /// </summary>
    /// <param name="builder">
    /// Constructor de configuración de la entidad.
    /// </param>
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable(
            "inventory_movements",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_inventory_movements_delta_non_zero",
                    "delta <> 0");
                table.HasCheckConstraint(
                    "ck_inventory_movements_result_non_negative",
                    "resulting_quantity >= 0");
            });
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).ValueGeneratedNever();
        builder.Property(movement => movement.InventoryLotId)
            .HasColumnName("inventory_lot_id");
        builder.Property(movement => movement.Type).HasColumnName("type");
        builder.Property(movement => movement.Delta)
            .HasColumnName("delta")
            .HasPrecision(12, 3);
        builder.Property(movement => movement.ResultingQuantity)
            .HasColumnName("resulting_quantity")
            .HasPrecision(12, 3);
        builder.Property(movement => movement.OccurredAt)
            .HasColumnName("occurred_at");
        builder.Property(movement => movement.MealPlanEntryId)
            .HasColumnName("meal_plan_entry_id");
        builder.HasIndex(movement => new { movement.InventoryLotId, movement.OccurredAt });
        builder.HasOne<MealPlanEntry>()
            .WithMany()
            .HasForeignKey(movement => movement.MealPlanEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
