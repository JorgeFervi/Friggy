using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecipes : Migration
{
    private static readonly string[] RecipeOrderColumns = { "recipe_id", "order" };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "recipes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                estimated_time = table.Column<TimeSpan>(type: "interval", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipes", x => x.Id);
                table.CheckConstraint("ck_recipes_estimated_time_non_negative", "estimated_time >= interval '0 minutes'");
            });

        migrationBuilder.CreateTable(
            name: "recipe_ingredients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                unit_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_ingredients", x => x.Id);
                table.CheckConstraint("ck_recipe_ingredients_order_non_negative", "\"order\" >= 0");
                table.CheckConstraint("ck_recipe_ingredients_quantity_positive", "quantity > 0");
                table.ForeignKey(
                    name: "FK_recipe_ingredients_ingredients_ingredient_id",
                    column: x => x.ingredient_id,
                    principalTable: "ingredients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipe_ingredients_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_recipe_ingredients_unit_types_unit_type_id",
                    column: x => x.unit_type_id,
                    principalTable: "unit_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "recipe_meal_types",
            columns: table => new
            {
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                meal_type_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_meal_types", x => new { x.recipe_id, x.meal_type_id });
                table.ForeignKey(
                    name: "FK_recipe_meal_types_meal_types_meal_type_id",
                    column: x => x.meal_type_id,
                    principalTable: "meal_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipe_meal_types_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipe_recipe_tags",
            columns: table => new
            {
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_tag_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_recipe_tags", x => new { x.recipe_id, x.recipe_tag_id });
                table.ForeignKey(
                    name: "FK_recipe_recipe_tags_recipe_tags_recipe_tag_id",
                    column: x => x.recipe_tag_id,
                    principalTable: "recipe_tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipe_recipe_tags_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipe_steps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                description = table.Column<string>(type: "text", nullable: false),
                estimated_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_steps", x => x.Id);
                table.CheckConstraint("ck_recipe_steps_estimated_time_non_negative", "estimated_time IS NULL OR estimated_time >= interval '0 minutes'");
                table.CheckConstraint("ck_recipe_steps_order_non_negative", "\"order\" >= 0");
                table.ForeignKey(
                    name: "FK_recipe_steps_recipes_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredients_ingredient_id",
            table: "recipe_ingredients",
            column: "ingredient_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredients_recipe_id_order",
            table: "recipe_ingredients",
            columns: RecipeOrderColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredients_unit_type_id",
            table: "recipe_ingredients",
            column: "unit_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_meal_types_meal_type_id",
            table: "recipe_meal_types",
            column: "meal_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_recipe_tags_recipe_tag_id",
            table: "recipe_recipe_tags",
            column: "recipe_tag_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_steps_recipe_id_order",
            table: "recipe_steps",
            columns: RecipeOrderColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipes_normalized_name",
            table: "recipes",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "recipe_ingredients");

        migrationBuilder.DropTable(
            name: "recipe_meal_types");

        migrationBuilder.DropTable(
            name: "recipe_recipe_tags");

        migrationBuilder.DropTable(
            name: "recipe_steps");

        migrationBuilder.DropTable(
            name: "recipes");
    }
}
