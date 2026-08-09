using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;
using Friggy.Infrastructure.Persistence.Repositories;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;

namespace Friggy.IntegrationTests.Persistence;

public sealed class WeeklyPlanRepositoryTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private static readonly DateOnly WeekStart = new(2026, 8, 3);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanRepository_SaveAndReload_PreservesOrderedAggregate()
    {
        var recipes = await CreateRecipesAsync("Desayuno", "Comida", "Cena");
        var plan = WeeklyPlan.Create("Semana 32", WeekStart, "  Menú familiar  ");
        plan.Assign(WeekStart.AddDays(2), CatalogSeedIds.Dinner, recipes[2].Id);
        plan.Assign(WeekStart, CatalogSeedIds.Breakfast, recipes[0].Id);
        plan.Assign(WeekStart.AddDays(1), CatalogSeedIds.Lunch, recipes[1].Id);

        await SavePlanAsync(plan);

        await using var verificationContext = Database.CreateDbContext();
        var repository = new WeeklyPlanRepository(verificationContext);
        var reloaded = await repository.GetByIdAsync(
            plan.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal("Semana 32", reloaded.Name.Value);
        Assert.Equal("SEMANA 32", reloaded.Name.Normalized);
        Assert.Equal(WeekStart, reloaded.StartDate);
        Assert.Equal("Menú familiar", reloaded.Description);
        Assert.Equal(
            [WeekStart, WeekStart.AddDays(1), WeekStart.AddDays(2)],
            reloaded.Entries.Select(entry => entry.Date));
        Assert.Equal(recipes.Select(recipe => recipe.Id), reloaded.Entries.Select(entry => entry.RecipeId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanRepository_List_ReturnsCompleteAggregatesWithoutTracking()
    {
        var recipe = Assert.Single(await CreateRecipesAsync("Gazpacho"));
        var plan = WeeklyPlan.Create("Semana 32", WeekStart, null);
        plan.Assign(WeekStart, CatalogSeedIds.Lunch, recipe.Id);
        await SavePlanAsync(plan);

        await using var context = Database.CreateDbContext();
        var repository = new WeeklyPlanRepository(context);

        var result = await repository.ListAsync(TestContext.Current.CancellationToken);

        var reloaded = Assert.Single(result);
        Assert.Single(reloaded.Entries);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanRepository_ReplaceCellAndReload_StoresSingleReplacement()
    {
        var recipes = await CreateRecipesAsync("Anterior", "Sustituta");
        var plan = WeeklyPlan.Create("Semana 32", WeekStart, null);
        plan.Assign(WeekStart, CatalogSeedIds.Lunch, recipes[0].Id);
        await SavePlanAsync(plan);

        await using (var context = Database.CreateDbContext())
        {
            var repository = new WeeklyPlanRepository(context);
            var tracked = await repository.GetByIdAsync(
                plan.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(tracked);

            tracked.Assign(WeekStart, CatalogSeedIds.Lunch, recipes[1].Id);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = Database.CreateDbContext();
        var verificationRepository = new WeeklyPlanRepository(verificationContext);
        var reloaded = await verificationRepository.GetByIdAsync(
            plan.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        var entry = Assert.Single(reloaded.Entries);
        Assert.Equal(recipes[1].Id, entry.RecipeId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanRepository_ConcurrentCellCollision_IsRejectedByDatabase()
    {
        var recipes = await CreateRecipesAsync("Primera", "Segunda");
        var plan = WeeklyPlan.Create("Semana 32", WeekStart, null);
        await SavePlanAsync(plan);

        await using (var firstContext = Database.CreateDbContext())
        await using (var secondContext = Database.CreateDbContext())
        {
            var firstRepository = new WeeklyPlanRepository(firstContext);
            var secondRepository = new WeeklyPlanRepository(secondContext);
            var firstCopy = await firstRepository.GetByIdAsync(
                plan.Id,
                TestContext.Current.CancellationToken);
            var secondCopy = await secondRepository.GetByIdAsync(
                plan.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(firstCopy);
            Assert.NotNull(secondCopy);

            firstCopy.Assign(WeekStart, CatalogSeedIds.Lunch, recipes[0].Id);
            secondCopy.Assign(WeekStart, CatalogSeedIds.Lunch, recipes[1].Id);
            await firstRepository.SaveChangesAsync(TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                secondRepository.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        await using var verificationContext = Database.CreateDbContext();
        var stored = await verificationContext.Set<MealPlanEntry>().SingleAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(recipes[0].Id, stored.RecipeId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanRepository_InvalidRecipeForeignKey_RollsBackAggregate()
    {
        var plan = WeeklyPlan.Create("Semana 32", WeekStart, null);
        plan.Assign(WeekStart, CatalogSeedIds.Lunch, Guid.NewGuid());

        await using (var context = Database.CreateDbContext())
        {
            var repository = new WeeklyPlanRepository(context);
            await repository.AddAsync(plan, TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                repository.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        await using var verificationContext = Database.CreateDbContext();
        Assert.False(await verificationContext.WeeklyPlans.AnyAsync(
            TestContext.Current.CancellationToken));
        Assert.False(await verificationContext.Set<MealPlanEntry>().AnyAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanReferenceRepository_ReturnsExistingReferencesAndOrderedMealTypes()
    {
        var recipe = Assert.Single(await CreateRecipesAsync("Gazpacho"));
        await using var context = Database.CreateDbContext();
        var repository = new WeeklyPlanReferenceRepository(context);

        var recipeExists = await repository.RecipeExistsAsync(
            recipe.Id,
            TestContext.Current.CancellationToken);
        var missingRecipeExists = await repository.RecipeExistsAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var mealTypeExists = await repository.MealTypeExistsAsync(
            CatalogSeedIds.Lunch,
            TestContext.Current.CancellationToken);
        var missingMealTypeExists = await repository.MealTypeExistsAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var mealTypes = await repository.ListMealTypesAsync(
            TestContext.Current.CancellationToken);

        Assert.True(recipeExists);
        Assert.False(missingRecipeExists);
        Assert.True(mealTypeExists);
        Assert.False(missingMealTypeExists);
        Assert.Equal([0, 1, 2], mealTypes.Select(mealType => mealType.Order));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private async Task<IReadOnlyList<Recipe>> CreateRecipesAsync(params string[] names)
    {
        var recipes = names.Select(name => Recipe.Create(name, TimeSpan.Zero)).ToArray();
        await using var context = Database.CreateDbContext();
        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return recipes;
    }

    private async Task SavePlanAsync(WeeklyPlan plan)
    {
        await using var context = Database.CreateDbContext();
        var repository = new WeeklyPlanRepository(context);
        await repository.AddAsync(plan, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
