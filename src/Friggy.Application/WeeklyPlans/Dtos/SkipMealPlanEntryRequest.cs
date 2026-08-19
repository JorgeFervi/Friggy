namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos necesarios para omitir una comida y registrar una alternativa opcional.
/// </summary>
public sealed record SkipMealPlanEntryRequest(
    string? Reason,
    string? AlternativeDescription);
