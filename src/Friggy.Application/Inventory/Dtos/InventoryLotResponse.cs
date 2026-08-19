namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Datos de respuesta de un lote de inventario, incluidos sus movimientos.
/// </summary>
public sealed record InventoryLotResponse(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal Quantity,
    DateOnly ExpirationDate,
    bool IsExpired,
    IReadOnlyList<InventoryMovementResponse> Movements);
