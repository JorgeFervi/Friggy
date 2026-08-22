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
        var stock = snapshot.Stock.ToDictionary(item => (item.IngredientId, item.UnitTypeId), item => item.AvailableQuantity);
        var items = snapshot.Demands
            .Select(demand =>
            {
                var available = stock.GetValueOrDefault((demand.IngredientId, demand.UnitTypeId));
                return new ShoppingListItemResponse(demand.IngredientId, demand.IngredientName, demand.UnitTypeId,
                    demand.UnitTypeName, demand.UnitSymbol, demand.RequiredQuantity, available,
                    Math.Max(0, demand.RequiredQuantity - available));
            })
            .OrderBy(item => item.IngredientName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.UnitTypeName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return new ShoppingListResponse(from, to, today, items);
    }
}
