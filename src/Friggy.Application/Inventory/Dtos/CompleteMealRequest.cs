namespace Friggy.Application.Inventory.Dtos;

public sealed record CompleteMealRequest(
    IReadOnlyList<InventoryLotAllocationRequest> Allocations);
