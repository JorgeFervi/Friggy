namespace Friggy.Application.Recipes.Dtos;

public sealed record RecipeResponse(
    Guid Id,
    string Name,
    int EstimatedMinutes,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    IReadOnlyList<RecipeStepResponse> Steps,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> MealTypeIds);
