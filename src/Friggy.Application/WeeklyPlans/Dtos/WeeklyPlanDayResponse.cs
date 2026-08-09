namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record WeeklyPlanDayResponse(
    DateOnly Date,
    IReadOnlyList<WeeklyPlanMealResponse> Meals);
