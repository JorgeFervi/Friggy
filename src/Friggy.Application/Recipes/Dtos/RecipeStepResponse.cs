namespace Friggy.Application.Recipes.Dtos;

public sealed record RecipeStepResponse(
    Guid Id,
    string Description,
    int? EstimatedMinutes,
    int Order);
