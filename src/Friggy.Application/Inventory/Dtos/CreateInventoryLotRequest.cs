namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Datos necesarios para crear un lote de inventario.
/// </summary>
public sealed record CreateInventoryLotRequest(
    Guid IngredientId,
    Guid UnitTypeId,
    decimal Quantity,
    DateOnly ExpirationDate);
