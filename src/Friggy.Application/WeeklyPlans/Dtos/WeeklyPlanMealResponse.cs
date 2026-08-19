namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos de respuesta de una comida dentro de un día del plan semanal.
/// </summary>
public sealed record WeeklyPlanMealResponse(
    Guid MealTypeId,
    string MealTypeName,
    int MealTypeOrder,
    Guid? RecipeId,
    int Servings,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    Guid SlotId,
    int SlotOrder,
    string? PlannedTime,
    DateTime? PreparationStartsAt,
    MealPlanEntryState Status,
    string? SkippedReason,
    string? AlternativeDescription);
