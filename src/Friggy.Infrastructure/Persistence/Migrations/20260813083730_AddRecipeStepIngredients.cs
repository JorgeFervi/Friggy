using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRecipeStepIngredients : Migration
{
    private static readonly string[] RecipeEntityColumns = ["recipe_id", "Id"];

    private static readonly string[] IngredientLinkColumns =
        ["recipe_id", "recipe_ingredient_id"];

    private static readonly string[] StepLinkColumns =
        ["recipe_id", "recipe_step_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "AK_recipe_steps_recipe_id_Id",
            table: "recipe_steps",
            columns: RecipeEntityColumns);

        migrationBuilder.AddUniqueConstraint(
            name: "AK_recipe_ingredients_recipe_id_Id",
            table: "recipe_ingredients",
            columns: RecipeEntityColumns);

        migrationBuilder.CreateTable(
            name: "recipe_step_ingredients",
            columns: table => new
            {
                recipe_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_recipe_step_ingredients",
                    x => new { x.recipe_step_id, x.recipe_ingredient_id });
                table.ForeignKey(
                    name: "FK_recipe_step_ingredients_recipe_ingredients_recipe_id_recipe~",
                    columns: x => new { x.recipe_id, x.recipe_ingredient_id },
                    principalTable: "recipe_ingredients",
                    principalColumns: RecipeEntityColumns,
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_recipe_step_ingredients_recipe_steps_recipe_id_recipe_step_~",
                    columns: x => new { x.recipe_id, x.recipe_step_id },
                    principalTable: "recipe_steps",
                    principalColumns: RecipeEntityColumns,
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_recipe_step_ingredients_recipe_id_recipe_ingredient_id",
            table: "recipe_step_ingredients",
            columns: IngredientLinkColumns);

        migrationBuilder.CreateIndex(
            name: "IX_recipe_step_ingredients_recipe_id_recipe_step_id",
            table: "recipe_step_ingredients",
            columns: StepLinkColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "recipe_step_ingredients");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_recipe_steps_recipe_id_Id",
            table: "recipe_steps");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_recipe_ingredients_recipe_id_Id",
            table: "recipe_ingredients");
    }
}
