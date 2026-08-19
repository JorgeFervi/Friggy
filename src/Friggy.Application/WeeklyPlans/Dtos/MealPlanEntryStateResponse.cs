namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Estado y metadatos de una asignación de comida.
/// </summary>
public sealed record MealPlanEntryStateResponse(
    Guid EntryId,
    MealPlanEntryState Status,
    DateTimeOffset? CompletedAt,
    string? SkippedReason,
    string? AlternativeDescription);
