using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record MealPlanEntryStateResponse(
    Guid EntryId,
    MealPlanEntryStatus Status,
    DateTimeOffset? CompletedAt,
    string? SkippedReason,
    string? AlternativeDescription);
