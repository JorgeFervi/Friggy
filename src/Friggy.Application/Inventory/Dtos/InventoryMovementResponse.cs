namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Datos de respuesta de un movimiento de inventario.
/// </summary>
public sealed record InventoryMovementResponse(
    Guid Id,
    InventoryMovementKind Type,
    decimal Delta,
    decimal ResultingQuantity,
    DateTimeOffset OccurredAt,
    Guid? MealPlanEntryId);
