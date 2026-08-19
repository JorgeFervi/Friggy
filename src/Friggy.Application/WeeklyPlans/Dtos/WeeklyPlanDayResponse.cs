namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos de respuesta de un día del plan semanal y sus comidas.
/// </summary>
public sealed record WeeklyPlanDayResponse(
    DateOnly Date,
    IReadOnlyList<WeeklyPlanMealResponse> Meals);
