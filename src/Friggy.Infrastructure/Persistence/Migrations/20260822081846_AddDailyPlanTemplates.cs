using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDailyPlanTemplates : Migration
{
    private static readonly string[] DailyPlanTemplateMealOrderColumns =
        ["daily_plan_template_id", "order"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "daily_plan_templates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_daily_plan_templates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "daily_plan_template_meals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                daily_plan_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                meal_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                servings = table.Column<int>(type: "integer", nullable: false),
                planned_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_daily_plan_template_meals", x => x.Id);
                table.UniqueConstraint("AK_daily_plan_template_meals_daily_plan_template_id_meal_type_~", x => new { x.daily_plan_template_id, x.meal_type_id });
                table.CheckConstraint("ck_daily_plan_template_meals_order_non_negative", "\"order\" >= 0");
                table.CheckConstraint("ck_daily_plan_template_meals_servings_positive", "servings > 0");
                table.ForeignKey(
                    name: "FK_daily_plan_template_meals_daily_plan_templates_daily_plan_t~",
                    column: x => x.daily_plan_template_id,
                    principalTable: "daily_plan_templates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_daily_plan_template_meals_meal_types_meal_type_id",
                    column: x => x.meal_type_id,
                    principalTable: "meal_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_daily_plan_template_meals_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_daily_plan_template_meals_daily_plan_template_id_order",
            table: "daily_plan_template_meals",
            columns: DailyPlanTemplateMealOrderColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_daily_plan_template_meals_meal_type_id",
            table: "daily_plan_template_meals",
            column: "meal_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_daily_plan_template_meals_recipe_id",
            table: "daily_plan_template_meals",
            column: "recipe_id");

        migrationBuilder.CreateIndex(
            name: "IX_daily_plan_templates_normalized_name",
            table: "daily_plan_templates",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "daily_plan_template_meals");

        migrationBuilder.DropTable(
            name: "daily_plan_templates");
    }
}
