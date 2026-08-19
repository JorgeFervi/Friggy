namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos de un paso recibidos al crear o actualizar una receta.
/// </summary>
public sealed record RecipeStepRequest(
    string Description,
    int? EstimatedMinutes,
    int Order,
    IReadOnlyList<Guid>? RecipeIngredientIds = null);
