using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DeferRecipeIngredientOrderUniqueness : Migration
{
    private static readonly string[] RecipeOrderColumns = ["recipe_id", "order"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_recipe_ingredients_recipe_id_order",
            table: "recipe_ingredients");

        migrationBuilder.Sql(
            """
            ALTER TABLE recipe_ingredients
            ADD CONSTRAINT uq_recipe_ingredients_recipe_id_order
            UNIQUE (recipe_id, "order")
            DEFERRABLE INITIALLY DEFERRED;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE recipe_ingredients
            DROP CONSTRAINT uq_recipe_ingredients_recipe_id_order;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredients_recipe_id_order",
            table: "recipe_ingredients",
            columns: RecipeOrderColumns,
            unique: true);
    }
}
