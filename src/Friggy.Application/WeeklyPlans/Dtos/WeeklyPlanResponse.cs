namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos completos de respuesta de un plan semanal.
/// </summary>
public sealed record WeeklyPlanResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Description,
    IReadOnlyList<WeeklyPlanDayResponse> Days);
