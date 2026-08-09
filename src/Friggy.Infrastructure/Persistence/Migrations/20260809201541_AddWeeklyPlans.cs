using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddWeeklyPlans : Migration
{
    private static readonly string[] WeeklyPlanCellColumns =
    {
            "weekly_plan_id",
            "date",
            "meal_type_id",
        };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "weekly_plans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                start_date = table.Column<DateOnly>(type: "date", nullable: false),
                description = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weekly_plans", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "meal_plan_entries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                weekly_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                date = table.Column<DateOnly>(type: "date", nullable: false),
                meal_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_meal_plan_entries", x => x.Id);
                table.ForeignKey(
                    name: "FK_meal_plan_entries_meal_types_meal_type_id",
                    column: x => x.meal_type_id,
                    principalTable: "meal_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_meal_plan_entries_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_meal_plan_entries_weekly_plans_weekly_plan_id",
                    column: x => x.weekly_plan_id,
                    principalTable: "weekly_plans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_meal_plan_entries_meal_type_id",
            table: "meal_plan_entries",
            column: "meal_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_meal_plan_entries_recipe_id",
            table: "meal_plan_entries",
            column: "recipe_id");

        migrationBuilder.CreateIndex(
            name: "IX_meal_plan_entries_weekly_plan_id_date_meal_type_id",
            table: "meal_plan_entries",
            columns: WeeklyPlanCellColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_weekly_plans_normalized_name",
            table: "weekly_plans",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "meal_plan_entries");

        migrationBuilder.DropTable(
            name: "weekly_plans");
    }
}
