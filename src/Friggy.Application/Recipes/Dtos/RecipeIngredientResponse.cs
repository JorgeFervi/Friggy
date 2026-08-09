namespace Friggy.Application.Recipes.Dtos;

public sealed record RecipeIngredientResponse(
    Guid Id,
    Guid IngredientId,
    Guid UnitTypeId,
    decimal Quantity,
    int Order);
