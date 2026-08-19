namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos necesarios para actualizar una receta, incluidos sus ingredientes,
/// pasos y clasificaciones.
/// </summary>
public sealed record UpdateRecipeRequest(
    string Name,
    int EstimatedMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    IReadOnlyList<RecipeStepRequest> Steps,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> MealTypeIds);
