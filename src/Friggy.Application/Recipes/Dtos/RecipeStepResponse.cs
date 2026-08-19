namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos de respuesta de un paso de preparación de una receta.
/// </summary>
public sealed record RecipeStepResponse(
    Guid Id,
    string Description,
    int? EstimatedMinutes,
    int Order,
    IReadOnlyList<Guid> RecipeIngredientIds);
