using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Friggy.IntegrationTests.Persistence;

public sealed class UnitConversionMigrationTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task LatestSchema_SeedsConversionMetadataAndRejectsInvalidFactor()
    {
        await using var context = Database.CreateDbContext();
        var gram = await context.UnitTypes.SingleAsync(
            item => item.Id == Friggy.Domain.Catalogs.CatalogSeedIds.Gram,
            TestContext.Current.CancellationToken);
        var tablespoon = await context.UnitTypes.SingleAsync(
            item => item.Id == Friggy.Domain.Catalogs.CatalogSeedIds.Tablespoon,
            TestContext.Current.CancellationToken);

        Assert.Equal(Friggy.Domain.Catalogs.MeasurementDimension.Mass, gram.MeasurementDimension);
        Assert.Equal(1m, gram.BaseUnitFactor);
        Assert.True(gram.CanUseForShopping);
        Assert.Equal(Friggy.Domain.Catalogs.MeasurementDimension.Volume, tablespoon.MeasurementDimension);
        Assert.Equal(15m, tablespoon.BaseUnitFactor);
        Assert.False(tablespoon.CanUseForShopping);

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE unit_types SET base_unit_factor = 0 WHERE \"Id\" = {gram.Id}",
                TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Upgrade_CustomReferencedUnit_PreservesQuantitiesAndUsesExactDefaults()
    {
        const string previousMigration = "20260822081846_AddDailyPlanTemplates";
        var unitId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        await using var context = Database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(previousMigration, TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO unit_types ("Id", name, normalized_name, symbol)
            VALUES ({unitId}, 'Medida propia', 'MEDIDA PROPIA', 'mp');
            INSERT INTO ingredients ("Id", name, normalized_name)
            VALUES ({ingredientId}, 'Ingrediente propio', 'INGREDIENTE PROPIO');
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({recipeId}, 'Receta propia', 'RECETA PROPIA', interval '0 minutes');
            INSERT INTO recipe_ingredients
                ("Id", recipe_id, ingredient_id, unit_type_id, quantity, "order")
            VALUES ({lineId}, {recipeId}, {ingredientId}, {unitId}, 2.5, 0);
            INSERT INTO inventory_lots
                ("Id", ingredient_id, unit_type_id, quantity, expiration_date, version)
            VALUES ({lotId}, {ingredientId}, {unitId}, 7.5, DATE '2026-09-01', {Guid.NewGuid()});
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var unit = await context.UnitTypes.SingleAsync(
            item => item.Id == unitId,
            TestContext.Current.CancellationToken);
        var recipeQuantity = await context.Set<Friggy.Domain.Recipes.RecipeIngredient>()
            .Where(item => item.Id == lineId)
            .Select(item => item.Quantity)
            .SingleAsync(TestContext.Current.CancellationToken);
        var lotQuantity = await context.InventoryLots
            .Where(item => item.Id == lotId)
            .Select(item => item.Quantity)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Friggy.Domain.Catalogs.MeasurementDimension.Unconverted, unit.MeasurementDimension);
        Assert.Equal(1m, unit.BaseUnitFactor);
        Assert.True(unit.CanUseForCooking);
        Assert.True(unit.CanUseForShopping);
        Assert.Equal(2.5m, recipeQuantity);
        Assert.Equal(7.5m, lotQuantity);
    }
}
