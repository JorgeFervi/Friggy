namespace Friggy.Application.Recipes.Dtos;

public sealed record CreateRecipeRequest(
    string Name,
    int EstimatedMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    IReadOnlyList<RecipeStepRequest> Steps,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> MealTypeIds);
