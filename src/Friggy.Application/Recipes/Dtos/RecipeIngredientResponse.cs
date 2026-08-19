namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos de respuesta de una línea de ingrediente de una receta.
/// </summary>
public sealed record RecipeIngredientResponse(
    Guid Id,
    Guid IngredientId,
    Guid UnitTypeId,
    decimal Quantity,
    int Order);
