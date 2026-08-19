namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Cantidad que permanece pendiente después de completar una comida.
/// </summary>
public sealed record MealCompletionRemainderResponse(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal RemainingQuantity);
