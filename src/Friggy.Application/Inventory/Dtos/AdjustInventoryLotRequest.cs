namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Datos necesarios para ajustar la cantidad real de un lote.
/// </summary>
public sealed record AdjustInventoryLotRequest(decimal ActualQuantity);
