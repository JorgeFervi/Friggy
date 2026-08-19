namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Cantidad de un lote que se propone utilizar en una operación de inventario.
/// </summary>
public sealed record InventoryLotAllocationRequest(Guid LotId, decimal Quantity);
