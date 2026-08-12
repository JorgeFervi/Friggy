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

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsCompleted => CompletedAt.HasValue;

    internal static MealPlanEntry Create(
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        Guid recipeId,
        int servings) =>
        new(Guid.NewGuid(), weeklyPlanId, date, mealTypeId, recipeId, servings);

    internal void Replace(Guid recipeId, int servings)
    {
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

        CompletedAt = completedAt;
    }
}
