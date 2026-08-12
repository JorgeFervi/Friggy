using Friggy.Domain.Catalogs;
using Friggy.Domain.WeeklyPlans;
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
    public async Task InitializeAsync_NewPhysicalDatabase_AppliesAllMigrations()
    {
        await using var context = Database.CreateDbContext();

        var appliedMigrations = await context.Database
            .GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            appliedMigrations,
            migration => Assert.EndsWith("_InitialInfrastructure", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddCatalogs", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddRecipes", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddWeeklyPlans", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddInventoryAndMealCompletion",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddDailyMealPlanSlots",
                migration,
                StringComparison.Ordinal));
        Assert.StartsWith("friggy_tests_", Database.DatabaseName, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResetAsync_StateCreatedByTest_RemovesStateAndReappliesMigrations()
    {
        await using (var context = Database.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE temporary_harness_state (id integer PRIMARY KEY)",
                TestContext.Current.CancellationToken);
        }

        await Database.ResetAsync(TestContext.Current.CancellationToken);

        await using var resetContext = Database.CreateDbContext();
        var connection = resetContext.Database.GetDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.temporary_harness_state') IS NULL";

        var tableWasRemoved = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        var appliedMigrations = await resetContext.Database
            .GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(true, tableWasRemoved);
        Assert.Equal(6, appliedMigrations.Count());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddInventoryMigration_PhaseSevenData_DefaultsServingsAndPreservesEntry()
    {
        const string phaseSevenMigration = "20260809201541_AddWeeklyPlans";
        var recipeId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 10);
        await using var context = Database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(
            phaseSevenMigration,
            TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({recipeId}, 'Receta anterior', 'RECETA ANTERIOR', interval '0 minutes');
            INSERT INTO weekly_plans ("Id", name, normalized_name, start_date, description)
            VALUES ({planId}, 'Semana anterior', 'SEMANA ANTERIOR', {date}, NULL);
            INSERT INTO meal_plan_entries ("Id", weekly_plan_id, date, meal_type_id, recipe_id)
            VALUES ({entryId}, {planId}, {date}, {CatalogSeedIds.Lunch}, {recipeId});
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var entry = await context.MealPlanEntries.SingleAsync(
            item => item.Id == entryId,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, entry.Servings);
        Assert.False(entry.IsCompleted);
        Assert.Contains(
            await context.Database.GetAppliedMigrationsAsync(
                TestContext.Current.CancellationToken),
            migration => migration.EndsWith(
                "_AddInventoryAndMealCompletion",
                StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddDailySlotsMigration_PhaseEightData_CreatesEquivalentCalendarAndPreservesEntry()
    {
        const string phaseEightMigration = "20260812063108_AddInventoryAndMealCompletion";
        var recipeId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 10);
        await using var context = Database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(
            phaseEightMigration,
            TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({recipeId}, 'Receta anterior', 'RECETA ANTERIOR', interval '0 minutes');
            INSERT INTO weekly_plans ("Id", name, normalized_name, start_date, description)
            VALUES ({planId}, 'Semana anterior', 'SEMANA ANTERIOR', {date}, NULL);
            INSERT INTO meal_plan_entries
                ("Id", weekly_plan_id, date, meal_type_id, recipe_id, servings, completed_at)
            VALUES ({entryId}, {planId}, {date}, {CatalogSeedIds.Lunch}, {recipeId}, 3, NULL);
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var slots = await context.Set<MealPlanSlot>()
            .Where(slot => slot.WeeklyPlanId == planId)
            .OrderBy(slot => slot.Date)
            .ThenBy(slot => slot.Order)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        var entry = await context.MealPlanEntries.SingleAsync(
            item => item.Id == entryId,
            TestContext.Current.CancellationToken);

        Assert.Equal(21, slots.Length);
        Assert.All(
            Enumerable.Range(0, 7).Select(date.AddDays),
            day => Assert.Equal(
                [CatalogSeedIds.Breakfast, CatalogSeedIds.Lunch, CatalogSeedIds.Dinner],
                slots.Where(slot => slot.Date == day).Select(slot => slot.MealTypeId)));
        Assert.Contains(
            slots,
            slot => slot.Date == entry.Date && slot.MealTypeId == entry.MealTypeId);
        Assert.Equal(3, entry.Servings);
        Assert.False(entry.IsCompleted);
    }
}
