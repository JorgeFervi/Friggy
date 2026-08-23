using Friggy.Application.ShoppingLists.Dtos;
using Friggy.Application.ShoppingLists.Interfaces;
using Friggy.Domain.DailyPlans;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class ShoppingListReadRepository(FriggyDbContext context) : IShoppingListReadRepository
{
    public async Task<ShoppingListSnapshot> GetSnapshotAsync(DateOnly from, DateOnly endDate, DateOnly today, CancellationToken cancellationToken)
    {
        var demands = await context.DailyPlans.AsNoTracking()
            .Where(plan => plan.Date >= from && plan.Date <= endDate)
            .Join(context.MealPlanEntries.AsNoTracking().Where(entry => entry.Status == MealPlanEntryStatus.Planned),
                plan => plan.Id, entry => entry.DailyPlanId, (plan, entry) => entry)
            .Join(context.Set<RecipeIngredient>().AsNoTracking(), entry => entry.RecipeId, line => line.RecipeId,
                (entry, line) => new { entry.Servings, Line = line })
            .Join(context.Ingredients.AsNoTracking(), item => item.Line.IngredientId, ingredient => ingredient.Id,
                (item, ingredient) => new { item.Servings, item.Line, Ingredient = ingredient })
            .Join(context.UnitTypes.AsNoTracking(), item => item.Line.UnitTypeId, unit => unit.Id,
                (item, unit) => new { item.Servings, item.Line, item.Ingredient, Unit = unit })
            .GroupBy(item => new
            {
                item.Ingredient.Id,
                IngredientName = item.Ingredient.Name.Value,
                UnitTypeId = item.Unit.Id,
                UnitTypeName = item.Unit.Name.Value,
                item.Unit.Symbol,
            })
            .Select(group => new PlannedIngredientDemand(group.Key.Id, group.Key.IngredientName,
                group.Key.UnitTypeId, group.Key.UnitTypeName, group.Key.Symbol,
                group.Sum(item => item.Line.Quantity * item.Servings)))
            .ToListAsync(cancellationToken);

        var stock = await context.InventoryLots.AsNoTracking()
            .Where(lot => lot.Quantity > 0 && lot.ExpirationDate >= today)
            .GroupBy(lot => new { lot.IngredientId, lot.UnitTypeId })
            .Select(group => new AvailableIngredientStock(group.Key.IngredientId, group.Key.UnitTypeId, group.Sum(lot => lot.Quantity)))
            .ToListAsync(cancellationToken);
        var units = await context.UnitTypes.AsNoTracking().ToListAsync(cancellationToken);
        return new ShoppingListSnapshot(demands, stock) { Units = units };
    }
}
