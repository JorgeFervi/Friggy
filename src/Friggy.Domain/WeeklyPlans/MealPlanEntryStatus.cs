namespace Friggy.Domain.WeeklyPlans;

/// <summary>
/// Enumeración con los estados posibles de una comida del plan semanal.
/// </summary>
public enum MealPlanEntryStatus
{
    /// <summary>
    /// La comida está planificada y todavía no se ha completado ni omitido.
    /// </summary>
    Planned = 0,
    /// <summary>
    /// La comida se ha completado.
    /// </summary>
    Completed = 1,
    /// <summary>
    /// La comida se ha omitido.
    /// </summary>
    Skipped = 2,
}
