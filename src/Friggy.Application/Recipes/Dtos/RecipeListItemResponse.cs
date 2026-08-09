namespace Friggy.Application.Recipes.Dtos;

public sealed record RecipeListItemResponse(
    Guid Id,
    string Name,
    int EstimatedMinutes);
