namespace Friggy.Application.Recipes.Dtos;

public sealed record RecipeStepRequest(
    string Description,
    int? EstimatedMinutes,
    int Order);
