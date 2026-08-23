namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos resumidos de una receta para listados.
/// </summary>
public sealed record RecipeListItemResponse(
    Guid Id,
    string Name,
    int EstimatedMinutes,
    IReadOnlyList<Guid>? IngredientIds = null,
    IReadOnlyList<Guid>? TagIds = null,
    IReadOnlyList<Guid>? MealTypeIds = null);
