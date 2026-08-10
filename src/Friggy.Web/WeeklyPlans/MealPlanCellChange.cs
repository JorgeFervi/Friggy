namespace Friggy.Web.WeeklyPlans;

public sealed record MealPlanCellChange(
    DateOnly Date,
    Guid MealTypeId,
    Guid? RecipeId);
