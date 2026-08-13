using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;
using Friggy.Infrastructure.Persistence.Repositories;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Friggy.IntegrationTests.Persistence;

public sealed class RecipeRepositoryTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeRepository_SaveAndReload_PreservesCompleteOrderedAggregate()
    {
        var catalogs = await CreateCatalogsAsync();
        var recipe = CreateCompleteRecipe(catalogs, "Gazpacho");
        var firstIngredient = recipe.Ingredients.Single(item => item.Order == 0);
        var secondIngredient = recipe.Ingredients.Single(item => item.Order == 1);
        var firstStep = recipe.Steps.Single(item => item.Order == 0);
        var secondStep = recipe.Steps.Single(item => item.Order == 1);
        recipe.AssignIngredientToStep(firstStep.Id, firstIngredient.Id);
        recipe.AssignIngredientToStep(secondStep.Id, firstIngredient.Id);
        recipe.AssignIngredientToStep(secondStep.Id, secondIngredient.Id);

        await using (var context = Database.CreateDbContext())
        {
            var repository = new RecipeRepository(context);
            await repository.AddAsync(recipe, TestContext.Current.CancellationToken);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = Database.CreateDbContext();
        var verificationRepository = new RecipeRepository(verificationContext);
        var reloaded = await verificationRepository.GetByIdAsync(
            recipe.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal("Gazpacho", reloaded.Name.Value);
        Assert.Equal("GAZPACHO", reloaded.Name.Normalized);
        Assert.Equal(TimeSpan.FromMinutes(20), reloaded.EstimatedTime);
        Assert.Equal([0, 1], reloaded.Ingredients.Select(item => item.Order));
        Assert.Equal([1.125m, 2m], reloaded.Ingredients.Select(item => item.Quantity));
        Assert.Equal([0, 1], reloaded.Steps.Select(item => item.Order));
        Assert.Equal(
            [reloaded.Ingredients[0].Id],
            reloaded.Steps[0].RecipeIngredientIds);
        Assert.Equal(
            [reloaded.Ingredients[0].Id, reloaded.Ingredients[1].Id],
            reloaded.Steps[1].RecipeIngredientIds);
        Assert.Equal(catalogs.Tag.Id, Assert.Single(reloaded.TagIds));
        Assert.Equal(CatalogSeedIds.Lunch, Assert.Single(reloaded.MealTypeIds));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeRepository_ReplaceAndReload_ReplacesChildrenAsOneAggregate()
    {
        var catalogs = await CreateCatalogsAsync();
        var recipe = CreateCompleteRecipe(catalogs, "Anterior");
        await SaveRecipeAsync(recipe);
        var oldIngredientIds = recipe.Ingredients.Select(item => item.Id).ToArray();
        var oldStepIds = recipe.Steps.Select(item => item.Id).ToArray();

        await using (var context = Database.CreateDbContext())
        {
            var repository = new RecipeRepository(context);
            var tracked = await repository.GetByIdAsync(
                recipe.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(tracked);
            var replacement = Recipe.Create("Nueva", TimeSpan.FromMinutes(30));
            var replacementIngredient = replacement.AddIngredient(
                catalogs.IngredientOne.Id,
                CatalogSeedIds.Gram,
                3m,
                0);
            var replacementStep = replacement.AddStep("Servir", null, 0);
            replacement.AssignIngredientToStep(
                replacementStep.Id,
                replacementIngredient.Id);
            replacement.AddMealType(CatalogSeedIds.Dinner);
            replacement.EnsureComplete();

            tracked.ReplaceWith(replacement);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = Database.CreateDbContext();
        var verificationRepository = new RecipeRepository(verificationContext);
        var reloaded = await verificationRepository.GetByIdAsync(
            recipe.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal("Nueva", reloaded.Name.Value);
        Assert.Equal(TimeSpan.FromMinutes(30), reloaded.EstimatedTime);
        Assert.Single(reloaded.Ingredients);
        Assert.Single(reloaded.Steps);
        Assert.Equal(
            [reloaded.Ingredients[0].Id],
            reloaded.Steps[0].RecipeIngredientIds);
        Assert.Empty(reloaded.TagIds);
        Assert.Equal(CatalogSeedIds.Dinner, Assert.Single(reloaded.MealTypeIds));
        Assert.DoesNotContain(reloaded.Ingredients, item => oldIngredientIds.Contains(item.Id));
        Assert.DoesNotContain(reloaded.Steps, item => oldStepIds.Contains(item.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeRepository_List_ReturnsOrderedChildrenWithoutTrackingGraph()
    {
        var catalogs = await CreateCatalogsAsync();
        var recipe = CreateCompleteRecipe(catalogs, "Gazpacho");
        var firstStep = recipe.Steps.Single(item => item.Order == 0);
        var firstIngredient = recipe.Ingredients.Single(item => item.Order == 0);
        recipe.AssignIngredientToStep(firstStep.Id, firstIngredient.Id);
        await SaveRecipeAsync(recipe);

        await using var context = Database.CreateDbContext();
        var repository = new RecipeRepository(context);

        var result = await repository.ListAsync(TestContext.Current.CancellationToken);

        var reloaded = Assert.Single(result);
        Assert.Equal([0, 1], reloaded.Ingredients.Select(item => item.Order));
        Assert.Equal([0, 1], reloaded.Steps.Select(item => item.Order));
        Assert.Equal(
            [reloaded.Ingredients[0].Id],
            reloaded.Steps[0].RecipeIngredientIds);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeStepIngredientLink_CrossRecipeReference_IsRejectedByDatabase()
    {
        var catalogs = await CreateCatalogsAsync();
        var firstRecipe = CreateCompleteRecipe(catalogs, "Primera");
        var secondRecipe = CreateCompleteRecipe(catalogs, "Segunda");
        await SaveRecipeAsync(firstRecipe);
        await SaveRecipeAsync(secondRecipe);

        await using var context = Database.CreateDbContext();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO recipe_step_ingredients
                    (recipe_id, recipe_step_id, recipe_ingredient_id)
                VALUES
                    ({firstRecipe.Id}, {firstRecipe.Steps[0].Id}, {secondRecipe.Ingredients[0].Id})
                """,
                TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeRepository_RemoveIngredientOrStep_DeletesOnlyTheirAssociations()
    {
        var catalogs = await CreateCatalogsAsync();
        var recipe = CreateCompleteRecipe(catalogs, "Gazpacho");
        var ingredientToRemove = recipe.Ingredients[0];
        var retainedIngredient = recipe.Ingredients[1];
        var stepToRemove = recipe.Steps[0];
        var retainedStep = recipe.Steps[1];
        recipe.AssignIngredientToStep(stepToRemove.Id, ingredientToRemove.Id);
        recipe.AssignIngredientToStep(retainedStep.Id, ingredientToRemove.Id);
        recipe.AssignIngredientToStep(retainedStep.Id, retainedIngredient.Id);
        await SaveRecipeAsync(recipe);

        await using (var context = Database.CreateDbContext())
        {
            var repository = new RecipeRepository(context);
            var tracked = await repository.GetByIdAsync(
                recipe.Id,
                TestContext.Current.CancellationToken);
            Assert.NotNull(tracked);

            Assert.True(tracked.RemoveIngredient(ingredientToRemove.Id));
            Assert.True(tracked.RemoveStep(stepToRemove.Id));
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = Database.CreateDbContext();
        var verificationRepository = new RecipeRepository(verificationContext);
        var reloaded = await verificationRepository.GetByIdAsync(
            recipe.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal(retainedIngredient.Id, Assert.Single(reloaded.Ingredients).Id);
        Assert.Equal(retainedStep.Id, Assert.Single(reloaded.Steps).Id);
        Assert.Equal([retainedIngredient.Id], reloaded.Steps[0].RecipeIngredientIds);
        Assert.Single(await verificationContext.Set<RecipeStepIngredientLink>()
            .ToArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeRepository_InvalidForeignKey_RollsBackCompleteSave()
    {
        var recipe = Recipe.Create("Inválida", TimeSpan.FromMinutes(10));
        recipe.AddIngredient(Guid.NewGuid(), CatalogSeedIds.Gram, 1m, 0);
        recipe.AddStep("Preparar", null, 0);
        recipe.EnsureComplete();

        await using (var context = Database.CreateDbContext())
        {
            var repository = new RecipeRepository(context);
            await repository.AddAsync(recipe, TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                repository.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        await using var verificationContext = Database.CreateDbContext();
        Assert.False(await verificationContext.Recipes.AnyAsync(
            TestContext.Current.CancellationToken));
        Assert.False(await verificationContext.Set<RecipeIngredient>().AnyAsync(
            TestContext.Current.CancellationToken));
        Assert.False(await verificationContext.Set<RecipeStep>().AnyAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CatalogInUse_DeleteIsRestrictedByDatabase()
    {
        var catalogs = await CreateCatalogsAsync();
        await SaveRecipeAsync(CreateCompleteRecipe(catalogs, "Gazpacho"));

        await using var context = Database.CreateDbContext();
        var ingredient = await context.Ingredients.SingleAsync(
            item => item.Id == catalogs.IngredientOne.Id,
            TestContext.Current.CancellationToken);
        context.Ingredients.Remove(ingredient);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeCatalogRepository_ExistingAndMissingIds_ReturnExpectedResults()
    {
        var catalogs = await CreateCatalogsAsync();
        await using var context = Database.CreateDbContext();
        var repository = new RecipeCatalogRepository(context);

        var ingredientsExist = await repository.IngredientsExistAsync(
            [catalogs.IngredientOne.Id, catalogs.IngredientTwo.Id],
            TestContext.Current.CancellationToken);
        var unitsExist = await repository.UnitTypesExistAsync(
            [CatalogSeedIds.Gram, CatalogSeedIds.Unit],
            TestContext.Current.CancellationToken);
        var tagsExist = await repository.TagsExistAsync(
            [catalogs.Tag.Id],
            TestContext.Current.CancellationToken);
        var mealTypesExist = await repository.MealTypesExistAsync(
            [CatalogSeedIds.Lunch],
            TestContext.Current.CancellationToken);
        var missingIngredientExists = await repository.IngredientsExistAsync(
            [catalogs.IngredientOne.Id, Guid.NewGuid()],
            TestContext.Current.CancellationToken);

        Assert.True(ingredientsExist);
        Assert.True(unitsExist);
        Assert.True(tagsExist);
        Assert.True(mealTypesExist);
        Assert.False(missingIngredientExists);
    }

    private async Task<RecipeCatalogs> CreateCatalogsAsync()
    {
        var ingredientOne = Ingredient.Create("Tomate");
        var ingredientTwo = Ingredient.Create("Pepino");
        var tag = RecipeTag.Create("Vegano");
        await using var context = Database.CreateDbContext();
        context.AddRange(ingredientOne, ingredientTwo, tag);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new RecipeCatalogs(ingredientOne, ingredientTwo, tag);
    }

    private async Task SaveRecipeAsync(Recipe recipe)
    {
        await using var context = Database.CreateDbContext();
        var repository = new RecipeRepository(context);
        await repository.AddAsync(recipe, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Recipe CreateCompleteRecipe(RecipeCatalogs catalogs, string name)
    {
        var recipe = Recipe.Create(name, TimeSpan.FromMinutes(20));
        recipe.AddIngredient(catalogs.IngredientTwo.Id, CatalogSeedIds.Unit, 2m, 1);
        recipe.AddIngredient(catalogs.IngredientOne.Id, CatalogSeedIds.Gram, 1.125m, 0);
        recipe.AddStep("Servir", null, 1);
        recipe.AddStep("Triturar", TimeSpan.FromMinutes(5), 0);
        recipe.AddTag(catalogs.Tag.Id);
        recipe.AddMealType(CatalogSeedIds.Lunch);
        recipe.EnsureComplete();
        return recipe;
    }

    private sealed record RecipeCatalogs(
        Ingredient IngredientOne,
        Ingredient IngredientTwo,
        RecipeTag Tag);
}
