namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos resumidos de un plan semanal para listados.
/// </summary>
public sealed record WeeklyPlanListItemResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);
