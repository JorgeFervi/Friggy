namespace Friggy.Domain.WeeklyPlans;

public sealed class MealPlanEntry
{
    private MealPlanEntry()
    {
    }

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

    public Guid Id { get; private set; }

    public Guid WeeklyPlanId { get; private set; }

    public DateOnly Date { get; private set; }

    public Guid MealTypeId { get; private set; }

    public Guid RecipeId { get; private set; }

    public int Servings { get; private set; }

    public MealPlanEntryStatus Status { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? SkippedReason { get; private set; }

    public string? AlternativeDescription { get; private set; }

    public bool IsCompleted => Status == MealPlanEntryStatus.Completed;

    public bool IsSkipped => Status == MealPlanEntryStatus.Skipped;

    internal static MealPlanEntry Create(
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        Guid recipeId,
        int servings) =>
        new(Guid.NewGuid(), weeklyPlanId, date, mealTypeId, recipeId, servings);

    internal void Replace(Guid recipeId, int servings)
    {
        EnsurePlanned("modificar");
        RecipeId = recipeId;
        Servings = servings;
    }

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

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
