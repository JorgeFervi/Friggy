using Friggy.Domain.Catalogs;

namespace Friggy.Domain.DailyPlans;

/// <summary>Representa una receta asignada a una comida del plan diario.</summary>
public sealed class MealPlanEntry
{
    private MealPlanEntry()
    {
    }

    private MealPlanEntry(
        Guid id,
        Guid dailyPlanId,
        Guid mealTypeId,
        Guid recipeId,
        int servings)
    {
        Id = id;
        DailyPlanId = dailyPlanId;
        MealTypeId = mealTypeId;
        RecipeId = recipeId;
        Servings = servings;
    }

    /// <summary>Código técnico de la asignación.</summary>
    public Guid Id { get; private set; }

    /// <summary>Código del plan diario propietario.</summary>
    public Guid DailyPlanId { get; private set; }

    /// <summary>Código del tipo de comida.</summary>
    public Guid MealTypeId { get; private set; }

    /// <summary>Código de la receta.</summary>
    public Guid RecipeId { get; private set; }

    /// <summary>Número de comensales/raciones.</summary>
    public int Servings { get; private set; }

    /// <summary>Estado de la asignación.</summary>
    public MealPlanEntryStatus Status { get; private set; }

    /// <summary>Instante de finalización.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Motivo de omisión.</summary>
    public string? SkippedReason { get; private set; }

    /// <summary>Alternativa usada al omitir.</summary>
    public string? AlternativeDescription { get; private set; }

    /// <summary>Indica si la comida está completada.</summary>
    public bool IsCompleted => Status == MealPlanEntryStatus.Completed;

    /// <summary>Indica si la comida está omitida.</summary>
    public bool IsSkipped => Status == MealPlanEntryStatus.Skipped;

    internal static MealPlanEntry Create(
        Guid dailyPlanId,
        Guid mealTypeId,
        Guid recipeId,
        int servings) =>
        new(Guid.NewGuid(), dailyPlanId, mealTypeId, recipeId, servings);

    internal void Replace(Guid recipeId, int servings)
    {
        EnsurePlanned("modificar");
        RecipeId = recipeId;
        Servings = servings;
    }

    internal void Complete(DateTimeOffset completedAt)
    {
        EnsurePlanned("completar");
        Status = MealPlanEntryStatus.Completed;
        CompletedAt = completedAt;
    }

    internal void Skip(string? reason, string? alternativeDescription)
    {
        EnsurePlanned("omitir");
        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            throw new DomainValidationException(
                "daily-plan.entry.skipped-reason.required",
                "El motivo de la omisión es obligatorio.");
        }

        Status = MealPlanEntryStatus.Skipped;
        SkippedReason = normalizedReason;
        AlternativeDescription = NormalizeOptionalText(alternativeDescription);
    }

    private void EnsurePlanned(string action)
    {
        if (IsCompleted)
        {
            throw new DomainValidationException(
                "daily-plan.entry.completed",
                $"No se puede {action} una comida completada.");
        }

        if (IsSkipped)
        {
            throw new DomainValidationException(
                "daily-plan.entry.skipped",
                $"No se puede {action} una comida omitida.");
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
