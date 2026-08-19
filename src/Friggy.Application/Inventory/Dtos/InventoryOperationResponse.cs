namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Resultado de una operación de inventario junto con el lote afectado.
/// </summary>
public sealed record InventoryOperationResponse(
    InventoryLotResponse Lot,
    decimal AppliedQuantity,
    decimal UnappliedQuantity);
