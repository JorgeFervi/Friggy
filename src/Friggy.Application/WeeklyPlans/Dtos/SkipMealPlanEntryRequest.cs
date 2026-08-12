namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record SkipMealPlanEntryRequest(
    string? Reason,
    string? AlternativeDescription);
