namespace Friggy.Application.DailyPlans.Dtos;

/// <summary>Solicitud para crear el plan de una fecha.</summary>
public sealed record CreateDailyPlanRequest(DateOnly Date);

/// <summary>Respuesta de un intervalo inclusivo.</summary>
public sealed record DailyPlanRangeResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyPlanResponse> Plans);

/// <summary>Detalle completo de un plan diario.</summary>
public sealed record DailyPlanResponse(
    Guid Id,
    DateOnly Date,
    IReadOnlyList<DailyPlanMealResponse> Meals);

/// <summary>Detalle de un hueco y su posible receta.</summary>
public sealed record DailyPlanMealResponse(
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

/// <summary>Estado expuesto de una asignación.</summary>
public enum MealPlanEntryState
{
    /// <summary>Planificada.</summary>
    Planned,

    /// <summary>Completada.</summary>
    Completed,

    /// <summary>Omitida.</summary>
    Skipped,
}

/// <summary>Solicitud para asignar una receta.</summary>
public sealed record SetMealPlanEntryRequest(Guid RecipeId, int Servings = 1);

/// <summary>Solicitud para añadir un hueco.</summary>
public sealed record AddMealPlanSlotRequest(Guid MealTypeId);

/// <summary>Solicitud para reordenar huecos.</summary>
public sealed record ReorderMealPlanSlotsRequest(IReadOnlyList<Guid> SlotIds);

/// <summary>Solicitud para cambiar la hora prevista.</summary>
public sealed record SetMealPlanSlotTimeRequest(string? PlannedTime);

/// <summary>Respuesta del horario calculado de un hueco.</summary>
public sealed record MealPlanSlotScheduleResponse(
    Guid SlotId,
    string? PlannedTime,
    DateTime? PreparationStartsAt);

/// <summary>Solicitud para omitir una comida.</summary>
public sealed record SkipMealPlanEntryRequest(
    string? Reason,
    string? AlternativeDescription);

/// <summary>Respuesta del cambio de estado de una comida.</summary>
public sealed record MealPlanEntryStateResponse(
    Guid EntryId,
    MealPlanEntryState Status,
    DateTimeOffset? CompletedAt,
    string? SkippedReason,
    string? AlternativeDescription);
