namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos completos de respuesta de una receta.
/// </summary>
public sealed record RecipeResponse(
    Guid Id,
    string Name,
    int EstimatedMinutes,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    IReadOnlyList<RecipeStepResponse> Steps,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> MealTypeIds);
