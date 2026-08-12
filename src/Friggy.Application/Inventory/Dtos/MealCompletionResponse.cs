namespace Friggy.Application.Inventory.Dtos;

public sealed record MealCompletionResponse(
    Guid MealPlanEntryId,
    bool AlreadyCompleted,
    IReadOnlyList<MealLotConsumptionResponse> Consumptions,
    IReadOnlyList<MealCompletionRemainderResponse>? RemainingRequirements = null);
