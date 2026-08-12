namespace Friggy.Application.Inventory.Dtos;

public sealed record CreateInventoryLotRequest(
    Guid IngredientId,
    Guid UnitTypeId,
    decimal Quantity,
    DateOnly ExpirationDate);
