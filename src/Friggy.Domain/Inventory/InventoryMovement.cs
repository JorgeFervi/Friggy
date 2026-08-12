namespace Friggy.Domain.Inventory;

public sealed class InventoryMovement
{
    private InventoryMovement()
    {
    }

    private InventoryMovement(
        Guid id,
        Guid inventoryLotId,
        InventoryMovementType type,
        decimal delta,
        decimal resultingQuantity,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId)
    {
        Id = id;
        InventoryLotId = inventoryLotId;
        Type = type;
        Delta = delta;
        ResultingQuantity = resultingQuantity;
        OccurredAt = occurredAt;
        MealPlanEntryId = mealPlanEntryId;
    }

    public Guid Id { get; private set; }

    public Guid InventoryLotId { get; private set; }

    public InventoryMovementType Type { get; private set; }

    public decimal Delta { get; private set; }

    public decimal ResultingQuantity { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? MealPlanEntryId { get; private set; }

    internal static InventoryMovement Create(
        Guid inventoryLotId,
        InventoryMovementType type,
        decimal delta,
        decimal resultingQuantity,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId = null) =>
        new(
            Guid.NewGuid(),
            inventoryLotId,
            type,
            delta,
            resultingQuantity,
            occurredAt,
            mealPlanEntryId);
}
