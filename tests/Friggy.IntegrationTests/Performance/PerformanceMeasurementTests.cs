using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Friggy.Application.Inventory.Services;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;
using Friggy.Domain.Inventory;
using Friggy.Domain.Recipes;
using Friggy.Infrastructure.Persistence;
using Friggy.Infrastructure.Persistence.Repositories;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Friggy.IntegrationTests.Performance;

public sealed class PerformanceMeasurementTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Fact]
    [Trait("Category", "Performance")]
    public async Task ReadModels_RecordDurationRowsAndSql()
    {
        var scenario = await SeedScenarioAsync();
        var measurements = new List<ScenarioMeasurement>();

        var recipeInterceptor = new SqlCaptureInterceptor();
        await using (var recipeContext = CreateMeasuredContext(recipeInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var recipes = await new RecipeRepository(recipeContext)
                .ListAsync(TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var recipeMeasurement = new ScenarioMeasurement(
                "recipes.list.aggregate-baseline",
                stopwatch.Elapsed.TotalMilliseconds,
                recipes.Count,
                recipes.Sum(recipe =>
                    recipe.Ingredients.Count +
                    recipe.Steps.Count +
                    recipe.Steps.Sum(step => step.IngredientLinks.Count) +
                    recipe.Tags.Count +
                    recipe.MealTypes.Count),
                recipeInterceptor.Commands.Count,
                recipeInterceptor.Commands);
            measurements.Add(recipeMeasurement);

            Assert.Equal(scenario.RecipeCount, recipeMeasurement.RootRows);
            Assert.Equal(6, recipeMeasurement.CommandCount);
            Assert.All(recipeMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var recipeSummaryInterceptor = new SqlCaptureInterceptor();
        await using (var recipeSummaryContext = CreateMeasuredContext(recipeSummaryInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var recipes = await new RecipeRepository(recipeSummaryContext)
                .ListSummariesAsync(TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var recipeSummaryMeasurement = new ScenarioMeasurement(
                "recipes.list.summary",
                stopwatch.Elapsed.TotalMilliseconds,
                recipes.Count,
                0,
                recipeSummaryInterceptor.Commands.Count,
                recipeSummaryInterceptor.Commands);
            measurements.Add(recipeSummaryMeasurement);

            Assert.Equal(scenario.RecipeCount, recipeSummaryMeasurement.RootRows);
            var firstRecipe = recipes.Single(item => item.Name == "Medición receta 00");
            Assert.Equal(15, firstRecipe.EstimatedMinutes);
            Assert.Equal(0, recipeSummaryMeasurement.RelatedRows);
            Assert.Equal(1, recipeSummaryMeasurement.CommandCount);
            Assert.All(recipeSummaryMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var dailyPlanInterceptor = new SqlCaptureInterceptor();
        await using (var dailyPlanContext = CreateMeasuredContext(dailyPlanInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var plans = await new DailyPlanRepository(dailyPlanContext)
                .ListBetweenAsync(
                    scenario.FirstPlanDate,
                    scenario.FirstPlanDate.AddDays(scenario.DailyPlanCount - 1),
                    TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var dailyPlanMeasurement = new ScenarioMeasurement(
                "daily-plans.list.range",
                stopwatch.Elapsed.TotalMilliseconds,
                plans.Count,
                plans.Sum(plan => plan.Entries.Count),
                dailyPlanInterceptor.Commands.Count,
                dailyPlanInterceptor.Commands);
            measurements.Add(dailyPlanMeasurement);

            Assert.Equal(scenario.DailyPlanCount, dailyPlanMeasurement.RootRows);
            Assert.Equal(
                scenario.DailyPlanCount * scenario.EntriesPerPlan,
                dailyPlanMeasurement.RelatedRows);
            Assert.Equal(3, dailyPlanMeasurement.CommandCount);
            Assert.All(dailyPlanMeasurement.Sql, sql =>
                Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var inventoryInterceptor = new SqlCaptureInterceptor();
        await using (var inventoryContext = CreateMeasuredContext(inventoryInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var lots = await new InventoryLotService(
                    new InventoryLotRepository(inventoryContext),
                    new InventoryReferenceRepository(inventoryContext),
                    new FixedTimeProvider())
                .ListAsync(false, TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var inventoryMeasurement = new ScenarioMeasurement(
                "inventory.list.available",
                stopwatch.Elapsed.TotalMilliseconds,
                lots.Count,
                lots.Sum(lot => lot.Movements.Count),
                inventoryInterceptor.Commands.Count,
                inventoryInterceptor.Commands);
            measurements.Add(inventoryMeasurement);

            Assert.Equal(scenario.InventoryLotCount, inventoryMeasurement.RootRows);
            Assert.Equal(scenario.InventoryLotCount, inventoryMeasurement.RelatedRows);
            Assert.Equal(3, inventoryMeasurement.CommandCount);
            Assert.All(inventoryMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var requirementsInterceptor = new SqlCaptureInterceptor();
        await using (var requirementsContext = CreateMeasuredContext(requirementsInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var requirements = await new DailyPlanInventoryService(
                    new DailyPlanRepository(requirementsContext),
                    new RecipeRepository(requirementsContext),
                    new InventoryLotRepository(requirementsContext),
                    new InventoryReferenceRepository(requirementsContext),
                    new InventoryUnitOfWork(requirementsContext),
                    new FixedTimeProvider())
                .GetRequirementsAsync(
                    scenario.FirstPlanDate,
                    TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var requirementsMeasurement = new ScenarioMeasurement(
                "daily-plans.inventory-requirements",
                stopwatch.Elapsed.TotalMilliseconds,
                requirements.Count,
                0,
                requirementsInterceptor.Commands.Count,
                requirementsInterceptor.Commands);
            measurements.Add(requirementsMeasurement);

            Assert.Equal(3, requirementsMeasurement.RootRows);
            Assert.Equal(12, requirementsMeasurement.CommandCount);
            Assert.All(requirementsMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var shoppingListInterceptor = new SqlCaptureInterceptor();
        await using (var shoppingListContext = CreateMeasuredContext(shoppingListInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var snapshot = await new ShoppingListReadRepository(shoppingListContext)
                .GetSnapshotAsync(
                    scenario.FirstPlanDate,
                    scenario.FirstPlanDate.AddDays(scenario.DailyPlanCount - 1),
                    new DateOnly(2026, 8, 12),
                    TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var shoppingListMeasurement = new ScenarioMeasurement(
                "shopping-list.snapshot",
                stopwatch.Elapsed.TotalMilliseconds,
                snapshot.Demands.Count,
                snapshot.Stock.Count,
                shoppingListInterceptor.Commands.Count,
                shoppingListInterceptor.Commands);
            measurements.Add(shoppingListMeasurement);

            Assert.Equal(3, shoppingListMeasurement.RootRows);
            Assert.Equal(3, shoppingListMeasurement.RelatedRows);
            Assert.Equal(2, shoppingListMeasurement.CommandCount);
            Assert.All(shoppingListMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var reportPath = Path.Combine(
            FindRepositoryRoot(),
            "TestResults",
            "performance",
            "8",
            "read-models.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(
                new
                {
                    generatedAtUtc = DateTimeOffset.UtcNow,
                    dataset = new
                    {
                        scenario.RecipeCount,
                        scenario.DailyPlanCount,
                        scenario.EntriesPerPlan,
                        scenario.InventoryLotCount,
                    },
                    measurements,
                },
                JsonOptions),
            TestContext.Current.CancellationToken);
    }

    private async Task<MeasurementScenario> SeedScenarioAsync()
    {
        var ingredients = Enumerable
            .Range(1, 3)
            .Select(index => Ingredient.Create($"Medición ingrediente {index}"))
            .ToArray();
        var tags = Enumerable
            .Range(1, 2)
            .Select(index => RecipeTag.Create($"Medición etiqueta {index}"))
            .ToArray();
        var mealTypes = Enumerable
            .Range(1, 3)
            .Select(index => MealType.Create($"Medición comida {index}", index - 1))
            .ToArray();
        var recipes = new List<Recipe>();
        var plans = new List<DailyPlan>();

        for (var recipeIndex = 0; recipeIndex < 12; recipeIndex++)
        {
            var recipe = Recipe.Create(
                $"Medición receta {recipeIndex:00}",
                TimeSpan.FromMinutes(15 + recipeIndex));
            for (var ingredientIndex = 0; ingredientIndex < ingredients.Length; ingredientIndex++)
            {
                recipe.AddIngredient(
                    ingredients[ingredientIndex].Id,
                    CatalogSeedIds.Gram,
                    ingredientIndex + 1,
                    ingredientIndex);
            }

            for (var stepIndex = 0; stepIndex < 3; stepIndex++)
            {
                recipe.AddStep($"Paso {stepIndex + 1}", TimeSpan.FromMinutes(stepIndex + 1), stepIndex);
            }

            foreach (var step in recipe.Steps)
            {
                var ingredient = recipe.Ingredients.Single(item => item.Order == step.Order);
                recipe.AssignIngredientToStep(step.Id, ingredient.Id);
            }

            foreach (var tag in tags)
            {
                recipe.AddTag(tag.Id);
            }

            foreach (var mealType in mealTypes)
            {
                recipe.AddMealType(mealType.Id);
            }

            recipe.EnsureComplete();
            recipes.Add(recipe);
        }

        const int dailyPlanCount = 8;
        const int entriesPerPlan = 3;
        var firstPlanDate = new DateOnly(2026, 8, 3);
        for (var planIndex = 0; planIndex < dailyPlanCount; planIndex++)
        {
            var plan = DailyPlan.Create(firstPlanDate.AddDays(planIndex));
            foreach (var mealType in mealTypes)
            {
                plan.Assign(
                    mealType.Id,
                    recipes[(planIndex + mealType.Order) % recipes.Count].Id);
            }

            plans.Add(plan);
        }

        var lots = Enumerable.Range(0, 30)
            .Select(index => InventoryLot.Create(
                ingredients[index % ingredients.Length].Id,
                CatalogSeedIds.Gram,
                10m,
                new DateOnly(2026, 8, 20).AddDays(index % 5),
                new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero)))
            .ToArray();

        await using var context = Database.CreateDbContext();
        context.AddRange(ingredients);
        context.AddRange(tags);
        context.AddRange(mealTypes);
        context.AddRange(recipes);
        context.AddRange(plans);
        context.AddRange(lots);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new MeasurementScenario(
            recipes.Count,
            plans.Count,
            entriesPerPlan,
            lots.Length,
            firstPlanDate);
    }

    private FriggyDbContext CreateMeasuredContext(SqlCaptureInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(
                Database.ConnectionString,
                postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName))
            .AddInterceptors(interceptor)
            .Options;
        return new FriggyDbContext(options);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Friggy.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }

    private sealed class SqlCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed record MeasurementScenario(
        int RecipeCount,
        int DailyPlanCount,
        int EntriesPerPlan,
        int InventoryLotCount,
        DateOnly FirstPlanDate);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset Current =
            new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Current;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed record ScenarioMeasurement(
        string Scenario,
        double DurationMilliseconds,
        int RootRows,
        int RelatedRows,
        int CommandCount,
        IReadOnlyList<string> Sql);
}
