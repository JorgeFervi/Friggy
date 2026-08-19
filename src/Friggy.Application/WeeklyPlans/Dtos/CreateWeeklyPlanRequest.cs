namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos necesarios para crear un plan semanal.
/// </summary>
public sealed record CreateWeeklyPlanRequest(
    string Name,
    DateOnly StartDate,
    string? Description);
