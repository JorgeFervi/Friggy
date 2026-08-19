namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Datos necesarios para completar una comida y distribuir su consumo entre
/// los lotes indicados.
/// </summary>
public sealed record CompleteMealRequest(
    IReadOnlyList<InventoryLotAllocationRequest> Allocations);
