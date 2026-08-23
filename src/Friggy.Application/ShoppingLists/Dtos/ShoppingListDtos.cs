using Friggy.Domain.Catalogs;

namespace Friggy.Application.ShoppingLists.Dtos;

public sealed record ShoppingListResponse(DateOnly From, DateOnly To, DateOnly CalculatedOn, IReadOnlyList<ShoppingListItemResponse> Items);

public sealed record ShoppingListItemResponse(
    Guid IngredientId, string IngredientName, Guid UnitTypeId, string UnitTypeName,
    string UnitSymbol, decimal RequiredQuantity, decimal AvailableQuantity, decimal QuantityToBuy);

public sealed record PlannedIngredientDemand(
    Guid IngredientId, string IngredientName, Guid UnitTypeId, string UnitTypeName,
    string UnitSymbol, decimal RequiredQuantity);

public sealed record AvailableIngredientStock(Guid IngredientId, Guid UnitTypeId, decimal AvailableQuantity);

public sealed record ShoppingListSnapshot(
    IReadOnlyList<PlannedIngredientDemand> Demands,
    IReadOnlyList<AvailableIngredientStock> Stock)
{
    public IReadOnlyList<UnitType> Units { get; init; } = [];
}
