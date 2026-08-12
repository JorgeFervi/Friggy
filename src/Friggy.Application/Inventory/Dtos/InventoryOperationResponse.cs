namespace Friggy.Application.Inventory.Dtos;

public sealed record InventoryOperationResponse(
    InventoryLotResponse Lot,
    decimal AppliedQuantity,
    decimal UnappliedQuantity);
