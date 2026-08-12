namespace Friggy.Application.Inventory.Dtos;

public sealed record InventoryMovementResponse(
    Guid Id,
    InventoryMovementKind Type,
    decimal Delta,
    decimal ResultingQuantity,
    DateTimeOffset OccurredAt,
    Guid? MealPlanEntryId);
