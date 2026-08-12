namespace Friggy.Application.Inventory.Dtos;

public sealed record MealCompletionRemainderResponse(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal RemainingQuantity);
