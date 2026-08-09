namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record WeeklyPlanResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Description,
    IReadOnlyList<WeeklyPlanDayResponse> Days);
