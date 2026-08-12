namespace Friggy.Domain.Inventory;

public readonly record struct InventoryOperationResult(
    decimal AppliedQuantity,
    decimal UnappliedQuantity);
