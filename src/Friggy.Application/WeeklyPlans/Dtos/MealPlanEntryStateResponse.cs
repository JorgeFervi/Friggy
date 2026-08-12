namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record MealPlanEntryStateResponse(
    Guid EntryId,
    MealPlanEntryState Status,
    DateTimeOffset? CompletedAt,
    string? SkippedReason,
    string? AlternativeDescription);
