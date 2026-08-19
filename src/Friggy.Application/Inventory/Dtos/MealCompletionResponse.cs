namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Resultado de completar una comida, con los consumos y las necesidades
/// que no pudieron cubrirse.
/// </summary>
public sealed record MealCompletionResponse(
    Guid MealPlanEntryId,
    bool AlreadyCompleted,
    IReadOnlyList<MealLotConsumptionResponse> Consumptions,
    IReadOnlyList<MealCompletionRemainderResponse>? RemainingRequirements = null);
