namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record WeeklyPlanMealResponse(
    Guid MealTypeId,
    string MealTypeName,
    int MealTypeOrder,
    Guid? RecipeId);
