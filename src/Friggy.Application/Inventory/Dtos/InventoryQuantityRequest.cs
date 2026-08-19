namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Cantidad solicitada para una operación de inventario.
/// </summary>
public sealed record InventoryQuantityRequest(decimal Quantity);
