using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;
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
                    recipe.Tags.Count +
                    recipe.MealTypes.Count),
                recipeInterceptor.Commands.Count,
                recipeInterceptor.Commands);
            measurements.Add(recipeMeasurement);

            Assert.Equal(scenario.RecipeCount, recipeMeasurement.RootRows);
            Assert.Equal(5, recipeMeasurement.CommandCount);
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

        var weeklyPlanInterceptor = new SqlCaptureInterceptor();
        await using (var weeklyPlanContext = CreateMeasuredContext(weeklyPlanInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var plans = await new WeeklyPlanRepository(weeklyPlanContext)
                .ListAsync(TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var weeklyPlanMeasurement = new ScenarioMeasurement(
                "weekly-plans.list.aggregate-baseline",
                stopwatch.Elapsed.TotalMilliseconds,
                plans.Count,
                plans.Sum(plan => plan.Entries.Count),
                weeklyPlanInterceptor.Commands.Count,
                weeklyPlanInterceptor.Commands);
            measurements.Add(weeklyPlanMeasurement);

            Assert.Equal(scenario.WeeklyPlanCount, weeklyPlanMeasurement.RootRows);
            Assert.Equal(scenario.WeeklyPlanCount * scenario.EntriesPerPlan, weeklyPlanMeasurement.RelatedRows);
            Assert.Equal(1, weeklyPlanMeasurement.CommandCount);
            Assert.All(weeklyPlanMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var weeklyPlanSummaryInterceptor = new SqlCaptureInterceptor();
        await using (var weeklyPlanSummaryContext = CreateMeasuredContext(weeklyPlanSummaryInterceptor))
        {
            var stopwatch = Stopwatch.StartNew();
            var plans = await new WeeklyPlanRepository(weeklyPlanSummaryContext)
                .ListSummariesAsync(TestContext.Current.CancellationToken);
            stopwatch.Stop();

            var weeklyPlanSummaryMeasurement = new ScenarioMeasurement(
                "weekly-plans.list.summary",
                stopwatch.Elapsed.TotalMilliseconds,
                plans.Count,
                0,
                weeklyPlanSummaryInterceptor.Commands.Count,
                weeklyPlanSummaryInterceptor.Commands);
            measurements.Add(weeklyPlanSummaryMeasurement);

            Assert.Equal(scenario.WeeklyPlanCount, weeklyPlanSummaryMeasurement.RootRows);
            var firstPlan = plans.Single(item => item.Name == "Medición plan 00");
            Assert.Equal(new DateOnly(2026, 8, 9), firstPlan.EndDate);
            Assert.Equal(0, weeklyPlanSummaryMeasurement.RelatedRows);
            Assert.Equal(1, weeklyPlanSummaryMeasurement.CommandCount);
            Assert.All(weeklyPlanSummaryMeasurement.Sql, sql => Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase));
        }

        var reportPath = Path.Combine(
            FindRepositoryRoot(),
            "TestResults",
            "performance",
            "6.5",
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
                        scenario.WeeklyPlanCount,
                        scenario.EntriesPerPlan,
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
        var plans = new List<WeeklyPlan>();

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

        const int weeklyPlanCount = 8;
        const int entriesPerPlan = 21;
        for (var planIndex = 0; planIndex < weeklyPlanCount; planIndex++)
        {
            var plan = WeeklyPlan.Create(
                $"Medición plan {planIndex:00}",
                new DateOnly(2026, 8, 3).AddDays(planIndex * 7),
                "Escenario de rendimiento");
            foreach (var date in plan.Dates)
            {
                foreach (var mealType in mealTypes)
                {
                    plan.Assign(
                        date,
                        mealType.Id,
                        recipes[(planIndex + date.DayNumber + mealType.Order) % recipes.Count].Id);
                }
            }

            plans.Add(plan);
        }

        await using var context = Database.CreateDbContext();
        context.AddRange(ingredients);
        context.AddRange(tags);
        context.AddRange(mealTypes);
        context.AddRange(recipes);
        context.AddRange(plans);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new MeasurementScenario(
            recipes.Count,
            plans.Count,
            entriesPerPlan);
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
        int WeeklyPlanCount,
        int EntriesPerPlan);

    private sealed record ScenarioMeasurement(
        string Scenario,
        double DurationMilliseconds,
        int RootRows,
        int RelatedRows,
        int CommandCount,
        IReadOnlyList<string> Sql);
}
