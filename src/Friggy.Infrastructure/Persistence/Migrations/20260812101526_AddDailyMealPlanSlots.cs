using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDailyMealPlanSlots : Migration
{
    private static readonly string[] MealPlanSlotCellColumns =
        ["weekly_plan_id", "date", "meal_type_id"];

    private static readonly string[] MealPlanSlotOrderColumns =
        ["weekly_plan_id", "date", "order"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "meal_plan_slots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                weekly_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateOnly>(type: "date", nullable: false),
                meal_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                order = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_meal_plan_slots", x => x.Id);
                table.UniqueConstraint(
                    "AK_meal_plan_slots_weekly_plan_id_date_meal_type_id",
                    x => new
                    {
                        x.weekly_plan_id,
                        x.date,
                        x.meal_type_id,
                    });
                table.CheckConstraint(
                    "ck_meal_plan_slots_order_non_negative",
                    "\"order\" >= 0");
                table.ForeignKey(
                    name: "FK_meal_plan_slots_meal_types_meal_type_id",
                    column: x => x.meal_type_id,
                    principalTable: "meal_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_meal_plan_slots_weekly_plans_weekly_plan_id",
                    column: x => x.weekly_plan_id,
                    principalTable: "weekly_plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO meal_plan_slots
                ("Id", weekly_plan_id, date, meal_type_id, "order")
            SELECT
                gen_random_uuid(),
                plan."Id",
                plan.start_date + day_offset,
                meal_type."Id",
                (ROW_NUMBER() OVER (
                    PARTITION BY plan."Id", day_offset
                    ORDER BY meal_type."order", meal_type.name, meal_type."Id") - 1)::integer
            FROM weekly_plans AS plan
            CROSS JOIN generate_series(0, 6) AS day_offset
            CROSS JOIN meal_types AS meal_type;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_meal_plan_slots_meal_type_id",
            table: "meal_plan_slots",
            column: "meal_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_meal_plan_slots_weekly_plan_id_date_order",
            table: "meal_plan_slots",
            columns: MealPlanSlotOrderColumns,
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_meal_plan_entries_meal_plan_slots",
            table: "meal_plan_entries",
            columns: MealPlanSlotCellColumns,
            principalTable: "meal_plan_slots",
            principalColumns: MealPlanSlotCellColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_meal_plan_entries_meal_plan_slots",
            table: "meal_plan_entries");

        migrationBuilder.DropTable(
            name: "meal_plan_slots");
    }
}
