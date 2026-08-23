using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Friggy.IntegrationTests.Persistence;

public sealed class WelcomeDataMigrationTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private const string PreviousMigration = "20260823092318_AddUnitConversionMetadata";

    private static readonly string[] ExpectedRecipeNames =
    [
        "Arroz con verduras",
        "Avena con plátano",
        "Ensalada de garbanzos",
        "Lentejas con verduras",
        "Pasta con tomate",
        "Pollo al horno con patatas",
        "Salmón al limón",
        "Tortilla francesa",
        "Tostada con tomate",
        "Yogur con fruta y nueces"
    ];

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EmptyInstallation_ContainsTenCompleteClassifiedWelcomeRecipes()
    {
        await using var context = Database.CreateDbContext();

        var recipes = await context.Recipes
            .AsNoTracking()
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Steps)
                .ThenInclude(step => step.IngredientLinks)
            .Include(recipe => recipe.Tags)
            .Include(recipe => recipe.MealTypes)
            .ToListAsync(TestContext.Current.CancellationToken);
        var tagNames = await context.RecipeTags
            .AsNoTracking()
            .Select(tag => tag.Name.Value)
            .ToListAsync(TestContext.Current.CancellationToken);
        var ingredientCount = await context.Ingredients
            .AsNoTracking()
            .CountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ExpectedRecipeNames, recipes
            .Select(recipe => recipe.Name.Value)
            .Order(StringComparer.Ordinal)
            .ToArray());
        Assert.Equal(25, ingredientCount);
        Assert.Equal(7, recipes.SelectMany(recipe => recipe.TagIds).Distinct().Count());
        Assert.Equal(3, recipes.SelectMany(recipe => recipe.MealTypeIds).Distinct().Count());
        Assert.All(recipes, recipe => Assert.NotEmpty(recipe.Ingredients));
        Assert.All(recipes, recipe => Assert.NotEmpty(recipe.Steps));
        Assert.All(recipes, recipe => Assert.NotEmpty(recipe.Tags));
        Assert.All(recipes, recipe => Assert.NotEmpty(recipe.MealTypes));
        Assert.All(
            recipes,
            recipe => Assert.All(
                recipe.Ingredients,
                ingredient => Assert.Contains(
                    recipe.Steps.SelectMany(step => step.RecipeIngredientIds),
                    id => id == ingredient.Id)));
        Assert.Contains("Rápida", tagNames);
        Assert.Contains("Vegetariana", tagNames);
        Assert.Contains("Vegana", tagNames);
        Assert.Contains("Sin gluten", tagNames);
        Assert.Contains("Al horno", tagNames);
        Assert.Contains("Alta en proteína", tagNames);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Upgrade_InstallationWithUserContent_DoesNotMixInWelcomeData()
    {
        await using var context = Database.CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            "DROP SCHEMA public CASCADE; CREATE SCHEMA public;",
            TestContext.Current.CancellationToken);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO ingredients ("Id", name, normalized_name)
            VALUES ({Guid.NewGuid()}, 'Ingrediente propio', 'INGREDIENTE PROPIO');
            INSERT INTO recipes ("Id", name, normalized_name, estimated_time)
            VALUES ({Guid.NewGuid()}, 'Receta propia', 'RECETA PROPIA', interval '15 minutes');
            """,
            TestContext.Current.CancellationToken);

        await migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);

        var recipeNames = await context.Recipes
            .AsNoTracking()
            .Select(recipe => recipe.Name.Value)
            .ToListAsync(TestContext.Current.CancellationToken);
        var ingredientNames = await context.Ingredients
            .AsNoTracking()
            .Select(ingredient => ingredient.Name.Value)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Receta propia"], recipeNames);
        Assert.Equal(["Ingrediente propio"], ingredientNames);
    }
}
