namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos necesarios para actualizar los detalles de un plan semanal.
/// </summary>
public sealed record UpdateWeeklyPlanRequest(string Name, string? Description);
