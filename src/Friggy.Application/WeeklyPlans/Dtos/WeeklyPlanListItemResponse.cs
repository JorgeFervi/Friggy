namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record WeeklyPlanListItemResponse(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);
