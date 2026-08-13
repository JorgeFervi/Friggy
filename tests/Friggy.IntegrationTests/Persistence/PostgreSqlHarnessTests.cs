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
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddMealPlanSlotSchedule",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddMealPlanEntrySkippedState",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_AddRecipeStepIngredients",
                migration,
                StringComparison.Ordinal),
            migration => Assert.EndsWith(
                "_DeferRecipeIngredientOrderUniqueness",
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
        Assert.Equal(10, appliedMigrations.Count());
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
        Assert.All(slots, slot => Assert.Null(slot.PlannedTime));
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddSkippedStateMigration_ExistingCompletionState_IsPreserved()
    {
        const string slotScheduleMigration = "20260812103001_AddMealPlanSlotSchedule";
        var recipeId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var plannedEntryId = Guid.NewGuid();
        var completedEntryId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 10);
        var completedAt = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        await using var context = Database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(
            slotScheduleMigration,
            TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({recipeId}, 'Receta anterior', 'RECETA ANTERIOR', interval '0 minutes');
            INSERT INTO weekly_plans ("Id", name, normalized_name, start_date, description)
            VALUES ({planId}, 'Semana anterior', 'SEMANA ANTERIOR', {date}, NULL);
            INSERT INTO meal_plan_slots
                ("Id", weekly_plan_id, date, meal_type_id, "order", planned_time)
            VALUES
                ({Guid.NewGuid()}, {planId}, {date}, {CatalogSeedIds.Breakfast}, 0, NULL),
                ({Guid.NewGuid()}, {planId}, {date}, {CatalogSeedIds.Lunch}, 1, NULL);
            INSERT INTO meal_plan_entries
                ("Id", weekly_plan_id, date, meal_type_id, recipe_id, servings, completed_at)
            VALUES
                ({plannedEntryId}, {planId}, {date}, {CatalogSeedIds.Breakfast}, {recipeId}, 1, NULL),
                ({completedEntryId}, {planId}, {date}, {CatalogSeedIds.Lunch}, {recipeId}, 1, {completedAt});
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var entries = await context.MealPlanEntries
            .Where(entry => entry.WeeklyPlanId == planId)
            .ToDictionaryAsync(entry => entry.Id, TestContext.Current.CancellationToken);
        Assert.Equal(MealPlanEntryStatus.Planned, entries[plannedEntryId].Status);
        Assert.Equal(MealPlanEntryStatus.Completed, entries[completedEntryId].Status);
        Assert.Equal(completedAt, entries[completedEntryId].CompletedAt);
        Assert.All(entries.Values, entry => Assert.False(entry.IsSkipped));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddRecipeStepIngredientsMigration_ExistingRecipes_RemainValidWithoutAssociations()
    {
        const string phaseNineMigration = "20260812104103_AddMealPlanEntrySkippedState";
        var recipeId = Guid.NewGuid();
        var recipeIngredientId = Guid.NewGuid();
        var recipeStepId = Guid.NewGuid();
        await using var context = Database.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(
            phaseNineMigration,
            TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO ingredients ("Id", name, normalized_name)
            VALUES ({Guid.NewGuid()}, 'Ingrediente anterior', 'INGREDIENTE ANTERIOR');
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({recipeId}, 'Receta anterior', 'RECETA ANTERIOR', interval '10 minutes');
            INSERT INTO recipe_ingredients
                ("Id", recipe_id, ingredient_id, unit_type_id, quantity, "order")
            SELECT
                {recipeIngredientId}, {recipeId}, "Id", {CatalogSeedIds.Gram}, 1, 0
            FROM ingredients
            WHERE normalized_name = 'INGREDIENTE ANTERIOR';
            INSERT INTO recipe_steps ("Id", recipe_id, description, estimated_time, "order")
            VALUES ({recipeStepId}, {recipeId}, 'Preparar', NULL, 0);
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var repository = new Friggy.Infrastructure.Persistence.Repositories.RecipeRepository(context);
        var recipe = await repository.GetByIdAsync(
            recipeId,
            TestContext.Current.CancellationToken);
        Assert.NotNull(recipe);
        Assert.Empty(Assert.Single(recipe.Steps).RecipeIngredientIds);
        Assert.Empty(await context.Set<Friggy.Domain.Recipes.RecipeStepIngredientLink>()
            .ToArrayAsync(TestContext.Current.CancellationToken));
    }
}
