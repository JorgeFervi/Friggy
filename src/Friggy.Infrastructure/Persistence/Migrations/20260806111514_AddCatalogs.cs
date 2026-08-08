using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddCatalogs : Migration
{
    private static readonly string[] UnitTypeColumns = { "Id", "name", "normalized_name", "symbol" };
    private static readonly string[] MealTypeColumns = { "Id", "name", "normalized_name", "order" };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ingredients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ingredients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "meal_types",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_meal_types", x => x.Id);
                table.CheckConstraint("ck_meal_types_order_non_negative", "\"order\" >= 0");
            });

        migrationBuilder.CreateTable(
            name: "recipe_tags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "unit_types",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_unit_types", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ingredients_normalized_name",
            table: "ingredients",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_meal_types_normalized_name",
            table: "meal_types",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipe_tags_normalized_name",
            table: "recipe_tags",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_unit_types_normalized_name",
            table: "unit_types",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_unit_types_symbol",
            table: "unit_types",
            column: "symbol",
            unique: true);

        migrationBuilder.InsertData(
            table: "unit_types",
            columns: UnitTypeColumns,
            values: new object[,]
            {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Gramo", "GRAMO", "g" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Kilogramo", "KILOGRAMO", "kg" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Mililitro", "MILILITRO", "ml" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Litro", "LITRO", "l" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Unidad", "UNIDAD", "ud" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Cucharadita", "CUCHARADITA", "cdta" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Cucharada", "CUCHARADA", "cda" }
            });

        migrationBuilder.InsertData(
            table: "meal_types",
            columns: MealTypeColumns,
            values: new object[,]
            {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "Desayuno", "DESAYUNO", 0 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "Comida", "COMIDA", 1 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "Cena", "CENA", 2 }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ingredients");

        migrationBuilder.DropTable(
            name: "meal_types");

        migrationBuilder.DropTable(
            name: "recipe_tags");

        migrationBuilder.DropTable(
            name: "unit_types");
    }
}
