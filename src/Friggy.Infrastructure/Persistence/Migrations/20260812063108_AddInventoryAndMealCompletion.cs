using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddInventoryAndMealCompletion : Migration
{
    private static readonly string[] InventoryLookupColumns =
        ["ingredient_id", "unit_type_id", "expiration_date"];

    private static readonly string[] MovementTimelineColumns =
        ["inventory_lot_id", "occurred_at"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "completed_at",
            table: "meal_plan_entries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "servings",
            table: "meal_plan_entries",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateTable(
            name: "inventory_lots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                unit_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                version = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_lots", x => x.Id);
                table.CheckConstraint("ck_inventory_lots_quantity_non_negative", "quantity >= 0");
                table.ForeignKey(
                    name: "FK_inventory_lots_ingredients_ingredient_id",
                    column: x => x.ingredient_id,
                    principalTable: "ingredients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_lots_unit_types_unit_type_id",
                    column: x => x.unit_type_id,
                    principalTable: "unit_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_movements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                inventory_lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<int>(type: "integer", nullable: false),
                delta = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                resulting_quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                meal_plan_entry_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_movements", x => x.Id);
                table.CheckConstraint("ck_inventory_movements_delta_non_zero", "delta <> 0");
                table.CheckConstraint("ck_inventory_movements_result_non_negative", "resulting_quantity >= 0");
                table.ForeignKey(
                    name: "FK_inventory_movements_inventory_lots_inventory_lot_id",
                    column: x => x.inventory_lot_id,
                    principalTable: "inventory_lots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_inventory_movements_meal_plan_entries_meal_plan_entry_id",
                    column: x => x.meal_plan_entry_id,
                    principalTable: "meal_plan_entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_meal_plan_entries_servings_positive",
            table: "meal_plan_entries",
            sql: "servings > 0");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_lots_ingredient_id_unit_type_id_expiration_date",
            table: "inventory_lots",
        columns: InventoryLookupColumns);

        migrationBuilder.CreateIndex(
            name: "IX_inventory_lots_unit_type_id",
            table: "inventory_lots",
            column: "unit_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_movements_inventory_lot_id_occurred_at",
            table: "inventory_movements",
        columns: MovementTimelineColumns);

        migrationBuilder.CreateIndex(
            name: "IX_inventory_movements_meal_plan_entry_id",
            table: "inventory_movements",
            column: "meal_plan_entry_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inventory_movements");

        migrationBuilder.DropTable(
            name: "inventory_lots");

        migrationBuilder.DropCheckConstraint(
            name: "ck_meal_plan_entries_servings_positive",
            table: "meal_plan_entries");

        migrationBuilder.DropColumn(
            name: "completed_at",
            table: "meal_plan_entries");

        migrationBuilder.DropColumn(
            name: "servings",
            table: "meal_plan_entries");
    }
}
