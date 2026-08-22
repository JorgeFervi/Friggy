using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Friggy.IntegrationTests.Persistence;

public sealed class PostgreSqlHarnessTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EmptyDatabase_AppliesFullChainAndCreatesDailySchema()
    {
        await using var context = Database.CreateDbContext();
        var migrations = (await context.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken)).ToArray();

        Assert.Equal(11, migrations.Length);
        Assert.EndsWith(
            "_ReplaceWeeklyPlansWithDailyPlans",
            migrations[^1],
            StringComparison.Ordinal);
        Assert.Equal(true, await ScalarAsync(
            context,
            "SELECT to_regclass('public.daily_plans') IS NOT NULL"));
        Assert.Equal(true, await ScalarAsync(
            context,
            "SELECT to_regclass('public.weekly_plans') IS NULL"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Reset_StateCreatedByTest_RemovesStateAndReappliesDailyMigration()
    {
        await using (var context = Database.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE temporary_harness_state (id integer PRIMARY KEY)",
                TestContext.Current.CancellationToken);
        }

        await Database.ResetAsync(TestContext.Current.CancellationToken);

        await using var resetContext = Database.CreateDbContext();
        Assert.Equal(true, await ScalarAsync(
            resetContext,
            "SELECT to_regclass('public.temporary_harness_state') IS NULL"));
        Assert.Equal(11, (await resetContext.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken)).Count());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReplacementMigration_ClearsOldPlanningLinkAndPreservesInventoryHistory()
    {
        const string previousMigration =
            "20260813085711_DeferRecipeIngredientOrderUniqueness";
        var recipeId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var movementId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var plannedDate = new DateOnly(2026, 8, 3);
        await using var context = Database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(previousMigration, TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({recipeId}, 'Receta anterior', 'RECETA ANTERIOR', interval '0 minutes');
            INSERT INTO ingredients ("Id", name, normalized_name)
            VALUES ({ingredientId}, 'Tomate anterior', 'TOMATE ANTERIOR');
            INSERT INTO weekly_plans ("Id", name, normalized_name, start_date, description)
            VALUES ({planId}, 'Semana anterior', 'SEMANA ANTERIOR', {plannedDate}, NULL);
            INSERT INTO meal_plan_slots
                ("Id", weekly_plan_id, date, meal_type_id, "order", planned_time)
            VALUES ({slotId}, {planId}, {plannedDate}, {CatalogSeedIds.Lunch}, 0, NULL);
            INSERT INTO meal_plan_entries
                ("Id", weekly_plan_id, date, meal_type_id, recipe_id, servings, status,
                 completed_at, skipped_reason, alternative_description)
            VALUES ({entryId}, {planId}, {plannedDate}, {CatalogSeedIds.Lunch}, {recipeId}, 1, 1,
                    now(), NULL, NULL);
            INSERT INTO inventory_lots
                ("Id", ingredient_id, unit_type_id, quantity, expiration_date, version)
            VALUES ({lotId}, {ingredientId}, {CatalogSeedIds.Gram}, 1, {plannedDate}, {Guid.NewGuid()});
            INSERT INTO inventory_movements
                ("Id", inventory_lot_id, type, delta, resulting_quantity, occurred_at,
                 meal_plan_entry_id)
            VALUES ({movementId}, {lotId}, 1, -1, 1, now(), {entryId});
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(true, await ScalarAsync(
            context,
            $"SELECT EXISTS (SELECT 1 FROM inventory_lots WHERE \"Id\" = '{lotId}')"));
        Assert.Equal(true, await ScalarAsync(
            context,
            $"SELECT meal_plan_entry_id IS NULL FROM inventory_movements WHERE \"Id\" = '{movementId}'"));
        Assert.False(await context.DailyPlans.AnyAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<object?> ScalarAsync(
        Friggy.Infrastructure.Persistence.FriggyDbContext context,
        string sql)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
    }
}
