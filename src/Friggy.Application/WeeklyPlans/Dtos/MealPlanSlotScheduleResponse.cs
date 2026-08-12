namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record MealPlanSlotScheduleResponse(
    Guid SlotId,
    string? PlannedTime,
    DateTime? PreparationStartsAt);
