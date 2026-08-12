namespace Friggy.Application.Inventory.Dtos;

public sealed record InventoryLotAllocationRequest(Guid LotId, decimal Quantity);
