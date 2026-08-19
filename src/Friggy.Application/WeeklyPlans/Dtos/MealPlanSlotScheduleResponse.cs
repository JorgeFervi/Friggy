namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Horario de un hueco de comida y comienzo calculado de su preparación.
/// </summary>
public sealed record MealPlanSlotScheduleResponse(
    Guid SlotId,
    string? PlannedTime,
    DateTime? PreparationStartsAt);
