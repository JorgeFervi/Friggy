namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record CreateWeeklyPlanRequest(
    string Name,
    DateOnly StartDate,
    string? Description);
