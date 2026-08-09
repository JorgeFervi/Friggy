using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;

namespace Friggy.Domain.Tests.Recipes;

public sealed class RecipeTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_BlankName_ThrowsValidation(string name)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Recipe.Create(name, TimeSpan.FromMinutes(20)));

        Assert.Equal("recipe.name.required", exception.Code);
    }

    [Fact]
    public void Create_NegativeEstimatedTime_ThrowsValidation()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Recipe.Create("Gazpacho", TimeSpan.FromMinutes(-1)));

        Assert.Equal("recipe.estimated-time.non-negative", exception.Code);
    }

    [Fact]
    public void Create_ValidValues_TrimsNameAndStartsEmpty()
    {
        var recipe = Recipe.Create(" Gazpacho ", TimeSpan.FromMinutes(20));

        Assert.NotEqual(Guid.Empty, recipe.Id);
        Assert.Equal("Gazpacho", recipe.Name.Value);
        Assert.Equal(TimeSpan.FromMinutes(20), recipe.EstimatedTime);
        Assert.Empty(recipe.Ingredients);
        Assert.Empty(recipe.Steps);
        Assert.Empty(recipe.TagIds);
        Assert.Empty(recipe.MealTypeIds);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void AddIngredient_NonPositiveQuantity_ThrowsAndDoesNotMutate(string value)
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddIngredient(
                Guid.NewGuid(),
                Guid.NewGuid(),
                decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
                order: 0));

        Assert.Equal("recipe-ingredient.quantity.positive", exception.Code);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void AddIngredient_DuplicateOrder_ThrowsConflictAndPreservesExistingIngredient()
    {
        var recipe = CreateRecipe();
        recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m, 0);

        var exception = Assert.Throws<RecipeConflictException>(() =>
            recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 2m, 0));

        Assert.Equal("recipe-ingredient.order.duplicate", exception.Code);
        Assert.Single(recipe.Ingredients);
        Assert.Equal(1m, recipe.Ingredients[0].Quantity);
    }

    [Fact]
    public void AddIngredient_EmptyCatalogId_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddIngredient(Guid.Empty, Guid.NewGuid(), 1m, 0));

        Assert.Equal("recipe-ingredient.ingredient-id.required", exception.Code);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void AddIngredient_EmptyUnitTypeId_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddIngredient(Guid.NewGuid(), Guid.Empty, 1m, 0));

        Assert.Equal("recipe-ingredient.unit-type-id.required", exception.Code);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void AddIngredient_NegativeOrder_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m, -1));

        Assert.Equal("recipe-ingredient.order.non-negative", exception.Code);
        Assert.Empty(recipe.Ingredients);
    }

    [Fact]
    public void RemoveIngredient_ExistingIngredient_RemovesOnlySelectedItem()
    {
        var recipe = CreateRecipe();
        recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m, 0);
        recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 2m, 1);
        var removedId = recipe.Ingredients[0].Id;

        var removed = recipe.RemoveIngredient(removedId);

        Assert.True(removed);
        var remaining = Assert.Single(recipe.Ingredients);
        Assert.Equal(2m, remaining.Quantity);
    }

    [Fact]
    public void AddStep_BlankDescription_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddStep(" ", null, 0));

        Assert.Equal("recipe-step.description.required", exception.Code);
        Assert.Empty(recipe.Steps);
    }

    [Fact]
    public void AddStep_NegativeTime_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddStep("Triturar", TimeSpan.FromMinutes(-1), 0));

        Assert.Equal("recipe-step.estimated-time.non-negative", exception.Code);
        Assert.Empty(recipe.Steps);
    }

    [Fact]
    public void AddStep_DuplicateOrder_ThrowsConflictAndPreservesExistingStep()
    {
        var recipe = CreateRecipe();
        recipe.AddStep("Triturar", null, 0);

        var exception = Assert.Throws<RecipeConflictException>(() =>
            recipe.AddStep("Servir", null, 0));

        Assert.Equal("recipe-step.order.duplicate", exception.Code);
        Assert.Equal("Triturar", Assert.Single(recipe.Steps).Description);
    }

    [Fact]
    public void AddStep_NegativeOrder_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var exception = Assert.Throws<DomainValidationException>(() =>
            recipe.AddStep("Triturar", null, -1));

        Assert.Equal("recipe-step.order.non-negative", exception.Code);
        Assert.Empty(recipe.Steps);
    }

    [Fact]
    public void AddTagAndMealType_DuplicateIds_ThrowConflicts()
    {
        var recipe = CreateRecipe();
        var tagId = Guid.NewGuid();
        var mealTypeId = Guid.NewGuid();
        recipe.AddTag(tagId);
        recipe.AddMealType(mealTypeId);

        var tagException = Assert.Throws<RecipeConflictException>(() => recipe.AddTag(tagId));
        var mealException = Assert.Throws<RecipeConflictException>(() => recipe.AddMealType(mealTypeId));

        Assert.Equal("recipe.tag.duplicate", tagException.Code);
        Assert.Equal("recipe.meal-type.duplicate", mealException.Code);
        Assert.Equal([tagId], recipe.TagIds);
        Assert.Equal([mealTypeId], recipe.MealTypeIds);
    }

    [Fact]
    public void AddTagOrMealType_EmptyId_ThrowsAndDoesNotMutate()
    {
        var recipe = CreateRecipe();

        var tagException = Assert.Throws<DomainValidationException>(() => recipe.AddTag(Guid.Empty));
        var mealException = Assert.Throws<DomainValidationException>(() => recipe.AddMealType(Guid.Empty));

        Assert.Equal("recipe.tag-id.required", tagException.Code);
        Assert.Equal("recipe.meal-type-id.required", mealException.Code);
        Assert.Empty(recipe.TagIds);
        Assert.Empty(recipe.MealTypeIds);
    }

    [Fact]
    public void RemoveStepTagAndMealType_ExistingValues_RemovesEachValue()
    {
        var recipe = CompleteRecipe("Gazpacho");
        var stepId = recipe.Steps[0].Id;
        var tagId = recipe.TagIds[0];
        var mealTypeId = recipe.MealTypeIds[0];

        Assert.True(recipe.RemoveStep(stepId));
        Assert.True(recipe.RemoveTag(tagId));
        Assert.True(recipe.RemoveMealType(mealTypeId));
        Assert.Empty(recipe.Steps);
        Assert.Empty(recipe.TagIds);
        Assert.Empty(recipe.MealTypeIds);
    }

    [Fact]
    public void Collections_CannotBeMutatedFromOutsideAggregate()
    {
        var recipe = CompleteRecipe("Gazpacho");

        Assert.Throws<NotSupportedException>(() =>
            ((IList<RecipeIngredient>)recipe.Ingredients).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<RecipeStep>)recipe.Steps).Clear());
        Assert.Single(recipe.Ingredients);
        Assert.Single(recipe.Steps);
    }

    [Fact]
    public void EnsureComplete_MissingIngredientOrStep_ThrowsSpecificValidation()
    {
        var withoutIngredient = CreateRecipe();
        withoutIngredient.AddStep("Servir", null, 0);
        var withoutStep = CreateRecipe();
        withoutStep.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m, 0);

        var ingredientException = Assert.Throws<DomainValidationException>(withoutIngredient.EnsureComplete);
        var stepException = Assert.Throws<DomainValidationException>(withoutStep.EnsureComplete);

        Assert.Equal("recipe.ingredients.required", ingredientException.Code);
        Assert.Equal("recipe.steps.required", stepException.Code);
    }

    [Fact]
    public void ReplaceWith_CompleteRecipe_ReplacesStateAndPreservesIdentity()
    {
        var recipe = CompleteRecipe("Anterior");
        var replacement = CompleteRecipe(" Nueva ");
        var id = recipe.Id;

        recipe.ReplaceWith(replacement);

        Assert.Equal(id, recipe.Id);
        Assert.Equal("Nueva", recipe.Name.Value);
        Assert.Equal(replacement.EstimatedTime, recipe.EstimatedTime);
        Assert.Equal(replacement.Ingredients.Select(item => item.Quantity), recipe.Ingredients.Select(item => item.Quantity));
        Assert.Equal(replacement.Steps.Select(item => item.Description), recipe.Steps.Select(item => item.Description));
        Assert.Equal(replacement.TagIds, recipe.TagIds);
        Assert.Equal(replacement.MealTypeIds, recipe.MealTypeIds);
        Assert.All(recipe.Ingredients, item => Assert.Equal(id, item.RecipeId));
        Assert.All(recipe.Steps, item => Assert.Equal(id, item.RecipeId));
    }

    [Fact]
    public void ReplaceWith_IncompleteRecipe_ThrowsAndPreservesOriginalState()
    {
        var recipe = CompleteRecipe("Original");
        var incomplete = CreateRecipe();
        var originalIngredientId = recipe.Ingredients[0].Id;

        var exception = Assert.Throws<DomainValidationException>(() => recipe.ReplaceWith(incomplete));

        Assert.Equal("recipe.ingredients.required", exception.Code);
        Assert.Equal("Original", recipe.Name.Value);
        Assert.Equal(originalIngredientId, Assert.Single(recipe.Ingredients).Id);
        Assert.Single(recipe.Steps);
    }

    private static Recipe CreateRecipe() => Recipe.Create("Gazpacho", TimeSpan.FromMinutes(20));

    private static Recipe CompleteRecipe(string name)
    {
        var recipe = Recipe.Create(name, TimeSpan.FromMinutes(20));
        recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 1.5m, 0);
        recipe.AddStep("Triturar", TimeSpan.FromMinutes(5), 0);
        recipe.AddTag(Guid.NewGuid());
        recipe.AddMealType(Guid.NewGuid());
        recipe.EnsureComplete();
        return recipe;
    }
}
