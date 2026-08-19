namespace Friggy.Domain.WeeklyPlans;

/// <summary>
/// Clase <see cref="MealPlanSlot"/> que representa un hueco de comida
/// disponible dentro de un día del plan semanal.
/// </summary>
public sealed class MealPlanSlot
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private MealPlanSlot()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar el hueco de forma interna.
    /// </param>
    /// <param name="weeklyPlanId">
    /// Código del plan semanal al que pertenece el hueco.
    /// </param>
    /// <param name="date">
    /// Día al que pertenece el hueco.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida del hueco.
    /// </param>
    /// <param name="order">
    /// Orden del hueco dentro del día.
    /// </param>
    private MealPlanSlot(
        Guid id,
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        int order)
    {
        Id = id;
        WeeklyPlanId = weeklyPlanId;
        Date = date;
        MealTypeId = mealTypeId;
        Order = order;
    }

    /// <summary>
    /// Código único para identificar el hueco de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código del plan semanal al que pertenece el hueco.
    /// </summary>
    public Guid WeeklyPlanId { get; private set; }

    /// <summary>
    /// Día al que pertenece el hueco.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Código del tipo de comida del hueco.
    /// </summary>
    public Guid MealTypeId { get; private set; }

    /// <summary>
    /// Orden del hueco dentro del día.
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// Hora prevista para preparar la comida.
    /// </summary>
    public TimeOnly? PlannedTime { get; private set; }

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="weeklyPlanId">
    /// Código del plan semanal al que pertenece el hueco.
    /// </param>
    /// <param name="date">
    /// Día al que pertenece el hueco.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida del hueco.
    /// </param>
    /// <param name="order">
    /// Orden del hueco dentro del día.
    /// </param>
    /// <returns>
    /// Objeto <see cref="MealPlanSlot"/>.
    /// </returns>
    internal static MealPlanSlot Create(
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        int order) =>
        new(Guid.NewGuid(), weeklyPlanId, date, mealTypeId, order);

    /// <summary>
    /// Método que cambia el orden del hueco dentro del día.
    /// </summary>
    /// <param name="order">
    /// Nuevo orden del hueco.
    /// </param>
    internal void MoveTo(int order) => Order = order;

    /// <summary>
    /// Método que establece la hora prevista para preparar la comida.
    /// </summary>
    /// <param name="plannedTime">
    /// Hora prevista para preparar la comida.
    /// </param>
    internal void SetPlannedTime(TimeOnly? plannedTime) => PlannedTime = plannedTime;

    /// <summary>
    /// Método que obtiene la fecha y hora de inicio de la preparación.
    /// </summary>
    /// <param name="estimatedTime">
    /// Tiempo estimado de preparación de la receta.
    /// </param>
    /// <returns>
    /// Fecha y hora de inicio o <see langword="null"/> cuando no hay hora
    /// prevista o tiempo estimado.
    /// </returns>
    /// <exception cref="Catalogs.DomainValidationException">
    /// Excepción de dominio lanzada cuando el tiempo estimado es negativo.
    /// </exception>
    public DateTime? GetPreparationStartsAt(TimeSpan? estimatedTime)
    {
        if (estimatedTime < TimeSpan.Zero)
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.slot.estimated-time.non-negative",
                "El tiempo estimado no puede ser negativo.");
        }

        if (!PlannedTime.HasValue || !estimatedTime.HasValue)
        {
            return null;
        }

        var plannedAt = Date.ToDateTime(
            PlannedTime.Value,
            DateTimeKind.Unspecified);
        return plannedAt.Subtract(estimatedTime.Value);
    }
}
