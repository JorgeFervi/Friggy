namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Resultado del consumo aplicado sobre un lote durante la finalización de
/// una comida.
/// </summary>
public sealed record MealLotConsumptionResponse(
    Guid LotId,
    decimal AppliedQuantity,
    decimal UnappliedQuantity);
