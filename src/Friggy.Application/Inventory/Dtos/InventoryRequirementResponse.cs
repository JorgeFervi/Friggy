namespace Friggy.Application.Inventory.Dtos;

public sealed record InventoryRequirementResponse(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal MissingQuantity);
