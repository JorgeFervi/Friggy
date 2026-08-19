namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Enumeración con los estados de una asignación de comida expuestos por
/// la aplicación.
/// </summary>
public enum MealPlanEntryState
{
    /// <summary>
    /// La comida está planificada.
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
