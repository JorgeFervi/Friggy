namespace Friggy.Domain.WeeklyPlans;

/// <summary>
/// Clase <see cref="MealPlanEntry"/> que representa una receta asignada a un
/// tipo de comida dentro de un día del plan semanal.
/// </summary>
public sealed class MealPlanEntry
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private MealPlanEntry()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar la asignación de forma interna.
    /// </param>
    /// <param name="weeklyPlanId">
    /// Código del plan semanal al que pertenece la asignación.
    /// </param>
    /// <param name="date">
    /// Día al que pertenece la asignación.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida asignado.
    /// </param>
    /// <param name="recipeId">
    /// Código de la receta asignada.
    /// </param>
    /// <param name="servings">
    /// Número de raciones.
    /// </param>
    private MealPlanEntry(
        Guid id,
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        Guid recipeId,
        int servings)
    {
        Id = id;
        WeeklyPlanId = weeklyPlanId;
        Date = date;
        MealTypeId = mealTypeId;
        RecipeId = recipeId;
        Servings = servings;
    }

    /// <summary>
    /// Código único para identificar la asignación de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código del plan semanal al que pertenece la asignación.
    /// </summary>
    public Guid WeeklyPlanId { get; private set; }

    /// <summary>
    /// Día al que pertenece la asignación.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Código del tipo de comida asignado.
    /// </summary>
    public Guid MealTypeId { get; private set; }

    /// <summary>
    /// Código de la receta asignada.
    /// </summary>
    public Guid RecipeId { get; private set; }

    /// <summary>
    /// Número de raciones de la receta.
    /// </summary>
    public int Servings { get; private set; }

    /// <summary>
    /// Estado actual de la asignación.
    /// </summary>
    public MealPlanEntryStatus Status { get; private set; }

    /// <summary>
    /// Fecha y hora en la que se completó la comida.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Motivo por el que se omitió la comida.
    /// </summary>
    public string? SkippedReason { get; private set; }

    /// <summary>
    /// Descripción opcional de la alternativa usada al omitir la comida.
    /// </summary>
    public string? AlternativeDescription { get; private set; }

    /// <summary>
    /// Indica si la asignación está completada.
    /// </summary>
    public bool IsCompleted => Status == MealPlanEntryStatus.Completed;

    /// <summary>
    /// Indica si la asignación está omitida.
    /// </summary>
    public bool IsSkipped => Status == MealPlanEntryStatus.Skipped;

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="weeklyPlanId">
    /// Código del plan semanal al que pertenece la asignación.
    /// </param>
    /// <param name="date">
    /// Día al que pertenece la asignación.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida asignado.
    /// </param>
    /// <param name="recipeId">
    /// Código de la receta asignada.
    /// </param>
    /// <param name="servings">
    /// Número de raciones.
    /// </param>
    /// <returns>
    /// Objeto <see cref="MealPlanEntry"/>.
    /// </returns>
    internal static MealPlanEntry Create(
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        Guid recipeId,
        int servings) =>
        new(Guid.NewGuid(), weeklyPlanId, date, mealTypeId, recipeId, servings);

    /// <summary>
    /// Método que reemplaza la receta y las raciones de una asignación planificada.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la nueva receta.
    /// </param>
    /// <param name="servings">
    /// Número de raciones de la nueva asignación.
    /// </param>
    /// <exception cref="Catalogs.DomainValidationException">
    /// Excepción de dominio lanzada cuando la asignación ya está completada
    /// u omitida.
    /// </exception>
    internal void Replace(Guid recipeId, int servings)
    {
        EnsurePlanned("modificar");
        RecipeId = recipeId;
        Servings = servings;
    }

    /// <summary>
    /// Método que marca la asignación como completada.
    /// </summary>
    /// <param name="completedAt">
    /// Fecha y hora en la que se completó la comida.
    /// </param>
    /// <exception cref="Catalogs.DomainValidationException">
    /// Excepción de dominio lanzada cuando la comida ya está completada u omitida.
    /// </exception>
    internal void Complete(DateTimeOffset completedAt)
    {
        if (IsCompleted)
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.entry.already-completed",
                "La comida ya está completada.");
        }

        if (IsSkipped)
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.entry.skipped",
                "No se puede completar una comida omitida.");
        }

        Status = MealPlanEntryStatus.Completed;
        CompletedAt = completedAt;
    }

    /// <summary>
    /// Método que marca la asignación como omitida y guarda el motivo.
    /// </summary>
    /// <param name="reason">
    /// Motivo de la omisión.
    /// </param>
    /// <param name="alternativeDescription">
    /// Descripción opcional de la alternativa usada.
    /// </param>
    /// <exception cref="Catalogs.DomainValidationException">
    /// Excepción de dominio lanzada cuando la asignación no está planificada
    /// o el motivo está vacío.
    /// </exception>
    internal void Skip(string? reason, string? alternativeDescription)
    {
        EnsurePlanned("omitir");
        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.entry.skipped-reason.required",
                "El motivo de la omisión es obligatorio.");
        }

        Status = MealPlanEntryStatus.Skipped;
        SkippedReason = normalizedReason;
        AlternativeDescription = NormalizeOptionalText(alternativeDescription);
    }

    /// <summary>
    /// Método que comprueba que la asignación todavía está planificada.
    /// </summary>
    /// <param name="action">
    /// Acción que se intenta realizar sobre la asignación.
    /// </param>
    /// <exception cref="Catalogs.DomainValidationException">
    /// Excepción de dominio lanzada cuando la asignación ya está completada
    /// u omitida.
    /// </exception>
    private void EnsurePlanned(string action)
    {
        if (IsCompleted)
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.entry.completed",
                $"No se puede {action} una comida completada.");
        }

        if (IsSkipped)
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.entry.skipped",
                $"No se puede {action} una comida omitida.");
        }
    }

    /// <summary>
    /// Método que normaliza un texto opcional eliminando espacios exteriores.
    /// </summary>
    /// <param name="value">
    /// Texto que se va a normalizar.
    /// </param>
    /// <returns>
    /// Texto normalizado o <see langword="null"/> cuando está vacío.
    /// </returns>
    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
