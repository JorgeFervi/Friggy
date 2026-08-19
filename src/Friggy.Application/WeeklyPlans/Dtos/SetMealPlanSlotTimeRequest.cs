namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Hora prevista que se quiere asignar a un hueco de comida.
/// </summary>
public sealed record SetMealPlanSlotTimeRequest(string? PlannedTime);
