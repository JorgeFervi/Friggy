using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Friggy.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUnitConversionMetadata : Migration
{
    private static readonly string[] ShoppingUnitIndexColumns =
        ["measurement_dimension", "can_use_for_shopping", "base_unit_factor"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "base_unit_factor",
            table: "unit_types",
            type: "numeric(18,9)",
            precision: 18,
            scale: 9,
            nullable: false,
            defaultValue: 1m);

        migrationBuilder.AddColumn<bool>(
            name: "can_use_for_cooking",
            table: "unit_types",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "can_use_for_shopping",
            table: "unit_types",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<short>(
            name: "measurement_dimension",
            table: "unit_types",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.Sql(
            """
                UPDATE unit_types
                SET measurement_dimension = CASE
                        WHEN "Id" IN (
                            '10000000-0000-0000-0000-000000000001',
                            '10000000-0000-0000-0000-000000000002') THEN 1
                        WHEN "Id" IN (
                            '10000000-0000-0000-0000-000000000003',
                            '10000000-0000-0000-0000-000000000004',
                            '10000000-0000-0000-0000-000000000006',
                            '10000000-0000-0000-0000-000000000007') THEN 2
                        WHEN "Id" = '10000000-0000-0000-0000-000000000005' THEN 3
                        ELSE 0
                    END,
                    base_unit_factor = CASE
                        WHEN "Id" IN (
                            '10000000-0000-0000-0000-000000000002',
                            '10000000-0000-0000-0000-000000000004') THEN 1000
                        WHEN "Id" = '10000000-0000-0000-0000-000000000006' THEN 5
                        WHEN "Id" = '10000000-0000-0000-0000-000000000007' THEN 15
                        ELSE 1
                    END,
                    can_use_for_cooking = TRUE,
                    can_use_for_shopping = "Id" NOT IN (
                        '10000000-0000-0000-0000-000000000006',
                        '10000000-0000-0000-0000-000000000007');
                """);

        migrationBuilder.CreateIndex(
            name: "IX_unit_types_measurement_dimension_can_use_for_shopping_base_~",
            table: "unit_types",
            columns: ShoppingUnitIndexColumns);

        migrationBuilder.AddCheckConstraint(
            name: "ck_unit_types_base_unit_factor_positive",
            table: "unit_types",
            sql: "base_unit_factor > 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_unit_types_measurement_dimension",
            table: "unit_types",
            sql: "measurement_dimension BETWEEN 0 AND 3");

        migrationBuilder.AddCheckConstraint(
            name: "ck_unit_types_unconverted_factor",
            table: "unit_types",
            sql: "measurement_dimension <> 0 OR base_unit_factor = 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_unit_types_measurement_dimension_can_use_for_shopping_base_~",
            table: "unit_types");

        migrationBuilder.DropCheckConstraint(
            name: "ck_unit_types_base_unit_factor_positive",
            table: "unit_types");

        migrationBuilder.DropCheckConstraint(
            name: "ck_unit_types_measurement_dimension",
            table: "unit_types");

        migrationBuilder.DropCheckConstraint(
            name: "ck_unit_types_unconverted_factor",
            table: "unit_types");

        migrationBuilder.DropColumn(
            name: "base_unit_factor",
            table: "unit_types");

        migrationBuilder.DropColumn(
            name: "can_use_for_cooking",
            table: "unit_types");

        migrationBuilder.DropColumn(
            name: "can_use_for_shopping",
            table: "unit_types");

        migrationBuilder.DropColumn(
            name: "measurement_dimension",
            table: "unit_types");
    }
}
