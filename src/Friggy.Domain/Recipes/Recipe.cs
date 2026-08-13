using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

public sealed class Recipe
{
    private readonly List<RecipeIngredient> ingredients = [];
    private readonly List<RecipeStep> steps = [];
    private readonly List<RecipeTagLink> tags = [];
    private readonly List<RecipeMealTypeLink> mealTypes = [];

    private Recipe()
    {
        Name = null!;
    }

    private Recipe(Guid id, CatalogName name, TimeSpan estimatedTime)
    {
        Id = id;
        Name = name;
        EstimatedTime = estimatedTime;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public TimeSpan EstimatedTime { get; private set; }

    public IReadOnlyList<RecipeIngredient> Ingredients => ingredients.AsReadOnly();

    public IReadOnlyList<RecipeStep> Steps => steps.AsReadOnly();

    public IReadOnlyList<RecipeTagLink> Tags => tags.AsReadOnly();

    public IReadOnlyList<RecipeMealTypeLink> MealTypes => mealTypes.AsReadOnly();

    public IReadOnlyList<Guid> TagIds => tags.Select(item => item.RecipeTagId).ToArray();

    public IReadOnlyList<Guid> MealTypeIds => mealTypes.Select(item => item.MealTypeId).ToArray();

    public static Recipe Create(string? name, TimeSpan estimatedTime)
    {
        ValidateEstimatedTime(estimatedTime);
        return new Recipe(
            Guid.NewGuid(),
            CatalogName.Create(name, "recipe.name.required"),
            estimatedTime);
    }

    public void AddIngredient(
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        int order)
    {
        var ingredient = RecipeIngredient.Create(
            Id,
            ingredientId,
            unitTypeId,
            quantity,
            order);

        if (ingredients.Any(item => item.Order == order))
        {
            throw new RecipeConflictException(
                "recipe-ingredient.order.duplicate",
                "No puede haber dos ingredientes en la misma posición.");
        }

        ingredients.Add(ingredient);
    }

    public bool RemoveIngredient(Guid recipeIngredientId)
    {
        var ingredient = ingredients.SingleOrDefault(item => item.Id == recipeIngredientId);
        if (ingredient is null)
        {
            return false;
        }

        foreach (var step in steps)
        {
            step.RemoveIngredient(recipeIngredientId);
        }

        return ingredients.Remove(ingredient);
    }

    public void AddStep(string? description, TimeSpan? estimatedTime, int order)
    {
        var step = RecipeStep.Create(Id, description, estimatedTime, order);

        if (steps.Any(item => item.Order == order))
        {
            throw new RecipeConflictException(
                "recipe-step.order.duplicate",
                "No puede haber dos pasos en la misma posición.");
        }

        steps.Add(step);
    }

    public bool RemoveStep(Guid recipeStepId)
    {
        var step = steps.SingleOrDefault(item => item.Id == recipeStepId);
        if (step is null)
        {
            return false;
        }

        step.ClearIngredientAssociations();
        return steps.Remove(step);
    }

    public void AssignIngredientToStep(Guid recipeStepId, Guid recipeIngredientId)
    {
        var step = GetStep(recipeStepId);
        var ingredient = GetIngredient(recipeIngredientId);
        step.AssignIngredient(ingredient);
    }

    public bool RemoveIngredientFromStep(Guid recipeStepId, Guid recipeIngredientId)
    {
        var step = GetStep(recipeStepId);
        _ = GetIngredient(recipeIngredientId);
        return step.RemoveIngredient(recipeIngredientId);
    }

    public void AddTag(Guid recipeTagId)
    {
        ValidateRequiredId(recipeTagId, "recipe.tag-id.required");
        if (tags.Any(item => item.RecipeTagId == recipeTagId))
        {
            throw new RecipeConflictException(
                "recipe.tag.duplicate",
                "La etiqueta ya está asignada a la receta.");
        }

        tags.Add(RecipeTagLink.Create(Id, recipeTagId));
    }

    public bool RemoveTag(Guid recipeTagId)
    {
        var tag = tags.SingleOrDefault(item => item.RecipeTagId == recipeTagId);
        return tag is not null && tags.Remove(tag);
    }

    public void AddMealType(Guid mealTypeId)
    {
        ValidateRequiredId(mealTypeId, "recipe.meal-type-id.required");
        if (mealTypes.Any(item => item.MealTypeId == mealTypeId))
        {
            throw new RecipeConflictException(
                "recipe.meal-type.duplicate",
                "El tipo de comida ya está asignado a la receta.");
        }

        mealTypes.Add(RecipeMealTypeLink.Create(Id, mealTypeId));
    }

    public bool RemoveMealType(Guid mealTypeId)
    {
        var mealType = mealTypes.SingleOrDefault(item => item.MealTypeId == mealTypeId);
        return mealType is not null && mealTypes.Remove(mealType);
    }

    public void EnsureComplete()
    {
        if (ingredients.Count == 0)
        {
            throw new DomainValidationException(
                "recipe.ingredients.required",
                "La receta debe contener al menos un ingrediente.");
        }

        if (steps.Count == 0)
        {
            throw new DomainValidationException(
                "recipe.steps.required",
                "La receta debe contener al menos un paso.");
        }
    }

    public void ReplaceWith(Recipe replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        replacement.EnsureComplete();

        var replacementName = CatalogName.Create(
            replacement.Name.Value,
            "recipe.name.required");
        ValidateEstimatedTime(replacement.EstimatedTime);

        var replacementIngredients = replacement.Ingredients
            .Select(item => RecipeIngredient.Create(
                Id,
                item.IngredientId,
                item.UnitTypeId,
                item.Quantity,
                item.Order))
            .ToArray();
        var replacementSteps = replacement.Steps
            .Select(item => RecipeStep.Create(
                Id,
                item.Description,
                item.EstimatedTime,
                item.Order))
            .ToArray();
        var replacementTags = replacement.TagIds
            .Select(tagId => RecipeTagLink.Create(Id, tagId))
            .ToArray();
        var replacementMealTypes = replacement.MealTypeIds
            .Select(mealTypeId => RecipeMealTypeLink.Create(Id, mealTypeId))
            .ToArray();

        Name = replacementName;
        EstimatedTime = replacement.EstimatedTime;
        ingredients.Clear();
        ingredients.AddRange(replacementIngredients);
        steps.Clear();
        steps.AddRange(replacementSteps);
        tags.Clear();
        tags.AddRange(replacementTags);
        mealTypes.Clear();
        mealTypes.AddRange(replacementMealTypes);
    }

    private static void ValidateEstimatedTime(TimeSpan estimatedTime)
    {
        if (estimatedTime < TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "recipe.estimated-time.non-negative",
                "El tiempo estimado no puede ser negativo.");
        }
    }

    private RecipeIngredient GetIngredient(Guid recipeIngredientId)
    {
        ValidateRequiredId(recipeIngredientId, "recipe-ingredient.id.required");
        return ingredients.SingleOrDefault(item => item.Id == recipeIngredientId) ??
            throw new DomainValidationException(
                "recipe-ingredient.not-found",
                "No se encontró la línea de ingrediente en la receta.");
    }

    private RecipeStep GetStep(Guid recipeStepId)
    {
        ValidateRequiredId(recipeStepId, "recipe-step.id.required");
        return steps.SingleOrDefault(item => item.Id == recipeStepId) ??
            throw new DomainValidationException(
                "recipe-step.not-found",
                "No se encontró el paso en la receta.");
    }

    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }
}
