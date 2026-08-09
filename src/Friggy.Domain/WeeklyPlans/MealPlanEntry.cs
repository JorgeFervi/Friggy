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
        Guid recipeId)
    {
        Id = id;
        WeeklyPlanId = weeklyPlanId;
        Date = date;
        MealTypeId = mealTypeId;
        RecipeId = recipeId;
    }

    public Guid Id { get; private set; }

    public Guid WeeklyPlanId { get; private set; }

    public DateOnly Date { get; private set; }

    public Guid MealTypeId { get; private set; }

    public Guid RecipeId { get; private set; }

    internal static MealPlanEntry Create(
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        Guid recipeId) =>
        new(Guid.NewGuid(), weeklyPlanId, date, mealTypeId, recipeId);

    internal void ReplaceRecipe(Guid recipeId) => RecipeId = recipeId;
}
