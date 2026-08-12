using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMealPlanEntrySkippedState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "alternative_description",
            table: "meal_plan_entries",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "skipped_reason",
            table: "meal_plan_entries",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "status",
            table: "meal_plan_entries",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            "UPDATE meal_plan_entries SET status = 1 WHERE completed_at IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "ck_meal_plan_entries_completion_state",
            table: "meal_plan_entries",
            sql: "(status = 1 AND completed_at IS NOT NULL) OR (status <> 1 AND completed_at IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_meal_plan_entries_skipped_state",
            table: "meal_plan_entries",
            sql: "(status = 2 AND skipped_reason IS NOT NULL AND btrim(skipped_reason) <> '') OR (status <> 2 AND skipped_reason IS NULL AND alternative_description IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_meal_plan_entries_status_valid",
            table: "meal_plan_entries",
            sql: "status IN (0, 1, 2)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_meal_plan_entries_completion_state",
            table: "meal_plan_entries");

        migrationBuilder.DropCheckConstraint(
            name: "ck_meal_plan_entries_skipped_state",
            table: "meal_plan_entries");

        migrationBuilder.DropCheckConstraint(
            name: "ck_meal_plan_entries_status_valid",
            table: "meal_plan_entries");

        migrationBuilder.DropColumn(
            name: "alternative_description",
            table: "meal_plan_entries");

        migrationBuilder.DropColumn(
            name: "skipped_reason",
            table: "meal_plan_entries");

        migrationBuilder.DropColumn(
            name: "status",
            table: "meal_plan_entries");
    }
}
