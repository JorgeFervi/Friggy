namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record WeeklyPlanMealResponse(
    Guid MealTypeId,
    string MealTypeName,
    int MealTypeOrder,
    Guid? RecipeId,
    int Servings = 1,
    bool IsCompleted = false,
    DateTimeOffset? CompletedAt = null,
    Guid SlotId = default,
    int SlotOrder = 0,
    string? PlannedTime = null,
    DateTime? PreparationStartsAt = null,
    MealPlanEntryState Status = MealPlanEntryState.Planned,
    string? SkippedReason = null,
    string? AlternativeDescription = null);
