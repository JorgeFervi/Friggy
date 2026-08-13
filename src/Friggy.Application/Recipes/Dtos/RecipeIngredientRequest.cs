namespace Friggy.Application.Recipes.Dtos;

public sealed record RecipeIngredientRequest(
    Guid IngredientId,
    Guid UnitTypeId,
    decimal Quantity,
    int Order,
    Guid? Id = null);
