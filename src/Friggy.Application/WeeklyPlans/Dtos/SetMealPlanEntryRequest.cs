namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record SetMealPlanEntryRequest(Guid RecipeId, int Servings = 1);
