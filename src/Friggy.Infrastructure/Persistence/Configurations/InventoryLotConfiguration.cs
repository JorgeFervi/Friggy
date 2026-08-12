using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Friggy.Infrastructure.Persistence.Configurations;

internal sealed class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable(
            "inventory_lots",
            table => table.HasCheckConstraint(
                "ck_inventory_lots_quantity_non_negative",
                "quantity >= 0"));
        builder.HasKey(lot => lot.Id);
        builder.Property(lot => lot.Id).ValueGeneratedNever();
        builder.Property(lot => lot.IngredientId).HasColumnName("ingredient_id");
        builder.Property(lot => lot.UnitTypeId).HasColumnName("unit_type_id");
        builder.Property(lot => lot.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(12, 3);
        builder.Property(lot => lot.ExpirationDate)
            .HasColumnName("expiration_date")
            .HasColumnType("date");
        builder.Property(lot => lot.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();
        builder.HasIndex(lot => new
        {
            lot.IngredientId,
            lot.UnitTypeId,
            lot.ExpirationDate,
        });
        builder.HasOne<Ingredient>()
            .WithMany()
            .HasForeignKey(lot => lot.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitType>()
            .WithMany()
            .HasForeignKey(lot => lot.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(lot => lot.Movements)
            .WithOne()
            .HasForeignKey(movement => movement.InventoryLotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
