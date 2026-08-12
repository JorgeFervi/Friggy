namespace Friggy.Application.Inventory.Dtos;

public enum InventoryMovementKind
{
    InitialStock = 0,
    Consumption = 1,
    Adjustment = 2,
    Discard = 3,
}
