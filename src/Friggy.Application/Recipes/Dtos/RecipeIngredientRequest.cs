namespace Friggy.Application.Recipes.Dtos;

/// <summary>
/// Datos de una línea de ingrediente recibidos al crear o actualizar una receta.
/// </summary>
public sealed record RecipeIngredientRequest(
    Guid IngredientId,
    Guid UnitTypeId,
    decimal Quantity,
    int Order,
    Guid? Id = null);
