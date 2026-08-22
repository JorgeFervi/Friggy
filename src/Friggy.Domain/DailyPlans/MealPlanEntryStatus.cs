namespace Friggy.Domain.DailyPlans;

/// <summary>Estado de una asignación de comida.</summary>
public enum MealPlanEntryStatus
{
    /// <summary>La comida está pendiente.</summary>
    Planned,

    /// <summary>La comida se ha completado.</summary>
    Completed,

    /// <summary>La comida se ha omitido.</summary>
    Skipped,
}
