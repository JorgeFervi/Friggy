namespace Friggy.Application.Inventory.Dtos;

public sealed record MealLotConsumptionResponse(
    Guid LotId,
    decimal AppliedQuantity,
    decimal UnappliedQuantity);
