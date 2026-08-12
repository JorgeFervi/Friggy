using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMealPlanSlotSchedule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<TimeOnly>(
            name: "planned_time",
            table: "meal_plan_slots",
            type: "time without time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "planned_time",
            table: "meal_plan_slots");
    }
}
