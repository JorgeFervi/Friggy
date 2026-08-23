using Friggy.Application.Measurements;
using Friggy.Application.ShoppingLists.Dtos;
using Friggy.Application.ShoppingLists.Interfaces;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.ShoppingLists.Services;

public sealed class ShoppingListService(IShoppingListReadRepository repository, TimeProvider timeProvider)
{
    public async Task<ShoppingListResponse> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw new DomainValidationException("shopping-list.date-range.invalid", "La fecha inicial no puede ser posterior a la final.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var snapshot = await repository.GetSnapshotAsync(from, to, today, cancellationToken);
        var items = snapshot.Units.Count == 0
            ? MapLegacy(snapshot)
            : MeasurementCalculator.Compare(
                    snapshot.Demands.Select(item => new MeasuredAmount(
                        item.IngredientId,
                        item.IngredientName,
                        item.UnitTypeId,
                        item.RequiredQuantity)),
                    snapshot.Stock.Select(item => new MeasuredAmount(
                        item.IngredientId,
                        string.Empty,
                        item.UnitTypeId,
                        item.AvailableQuantity)),
                    snapshot.Units)
                .Select(item => new ShoppingListItemResponse(
                    item.IngredientId,
                    item.IngredientName,
                    item.Unit.Id,
                    item.Unit.Name.Value,
                    item.Unit.Symbol,
                    item.Required,
                    item.Available,
                    item.Missing))
            .OrderBy(item => item.IngredientName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.UnitTypeName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return new ShoppingListResponse(from, to, today, items);
    }

    private static ShoppingListItemResponse[] MapLegacy(ShoppingListSnapshot snapshot)
    {
        var stock = snapshot.Stock.ToDictionary(
            item => (item.IngredientId, item.UnitTypeId),
            item => item.AvailableQuantity);
        return snapshot.Demands.Select(demand =>
            {
                var available = stock.GetValueOrDefault(
                    (demand.IngredientId, demand.UnitTypeId));
                return new ShoppingListItemResponse(
                    demand.IngredientId,
                    demand.IngredientName,
                    demand.UnitTypeId,
                    demand.UnitTypeName,
                    demand.UnitSymbol,
                    demand.RequiredQuantity,
                    available,
                    Math.Max(0, demand.RequiredQuantity - available));
            })
            .ToArray();
    }
}
