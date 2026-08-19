namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Necesidad de inventario de un ingrediente para una comida o planificación.
/// </summary>
public sealed record InventoryRequirementResponse(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal MissingQuantity);
