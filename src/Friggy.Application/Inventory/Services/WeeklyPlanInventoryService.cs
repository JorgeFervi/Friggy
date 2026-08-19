using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Application.WeeklyPlans.Exceptions;
using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.Inventory.Services;

/// <summary>
/// Servicio de aplicación que calcula las necesidades de inventario de un plan
/// y coordina la finalización de sus comidas.
/// </summary>
public sealed class WeeklyPlanInventoryService(
    IWeeklyPlanRepository plans,
    IRecipeRepository recipes,
    IInventoryLotRepository lots,
    IInventoryReferenceRepository references,
    IInventoryUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Calcula las necesidades de ingredientes de un plan semanal y las compara
    /// con las existencias disponibles.
    /// </summary>
    public async Task<IReadOnlyList<InventoryRequirementResponse>> GetRequirementsAsync(
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await FindPlanAsync(planId, cancellationToken);
        var requirements = await CalculateRequirementsAsync(
            plan.Entries,
            cancellationToken);
        var inventory = await lots.ListAsync(cancellationToken);
        var today = Today;
        var available = inventory
            .Where(lot => lot.Quantity > 0 && lot.ExpirationDate >= today)
            .GroupBy(lot => (lot.IngredientId, lot.UnitTypeId))
            .ToDictionary(group => group.Key, group => group.Sum(lot => lot.Quantity));
        return await MapRequirementsAsync(requirements, available, cancellationToken);
    }

    /// <summary>
    /// Completa una comida, consume las cantidades asignadas de los lotes y
    /// devuelve las necesidades que permanecen pendientes.
    /// </summary>
    public async Task<MealCompletionResponse> CompleteMealAsync(
        Guid planId,
        DateOnly date,
        Guid mealTypeId,
        CompleteMealRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Allocations);
        var plan = await FindPlanAsync(planId, cancellationToken);
        var entry = plan.Entries.SingleOrDefault(item =>
            item.Date == date && item.MealTypeId == mealTypeId) ??
            throw new InventoryNotFoundException(
                "meal-completion.entry.not-found",
                "No hay una receta asignada a la comida.");
        if (entry.IsCompleted)
        {
            return new MealCompletionResponse(entry.Id, true, [], []);
        }

        if (entry.IsSkipped)
        {
            throw new InventoryConflictException(
                "meal-completion.entry.skipped",
                "No se puede completar una comida omitida.");
        }

        var recipe = await recipes.GetByIdAsync(entry.RecipeId, cancellationToken) ??
            throw new InventoryNotFoundException(
                "meal-completion.recipe.not-found",
                "No se encontró la receta asignada.");
        var required = AggregateIngredients(recipe.Ingredients, entry.Servings);
        var inventory = await lots.ListForUpdateAsync(cancellationToken);
        var lotsById = inventory.ToDictionary(lot => lot.Id);
        var allocations = request.Allocations
            .GroupBy(allocation => allocation.LotId)
            .Select(group => new InventoryLotAllocationRequest(
                group.Key,
                group.Sum(allocation => allocation.Quantity)))
            .ToArray();
        ValidateAllocations(allocations, lotsById, required);

        var consumptions = new List<MealLotConsumptionResponse>(allocations.Length);
        foreach (var allocation in allocations)
        {
            var result = lotsById[allocation.LotId].Consume(
                allocation.Quantity,
                timeProvider.GetUtcNow(),
                entry.Id);
            consumptions.Add(new MealLotConsumptionResponse(
                allocation.LotId,
                result.AppliedQuantity,
                result.UnappliedQuantity));
        }

        var remaining = await MapRemaindersAsync(
            required,
            consumptions,
            lotsById,
            cancellationToken);
        plan.CompleteEntry(date, mealTypeId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MealCompletionResponse(entry.Id, false, consumptions, remaining);
    }

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private async Task<WeeklyPlan> FindPlanAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await plans.GetByIdAsync(id, cancellationToken) ??
        throw new WeeklyPlanNotFoundException(
            "weekly-plan.not-found",
            "No se encontró el plan semanal.");

    private async Task<Dictionary<(Guid IngredientId, Guid UnitTypeId), decimal>>
        CalculateRequirementsAsync(
            IReadOnlyCollection<MealPlanEntry> entries,
            CancellationToken cancellationToken)
    {
        var recipesById = (await recipes.ListAsync(cancellationToken))
            .ToDictionary(recipe => recipe.Id);
        var requirements = new Dictionary<(Guid, Guid), decimal>();
        foreach (var entry in entries)
        {
            if (entry.IsSkipped)
            {
                continue;
            }

            if (!recipesById.TryGetValue(entry.RecipeId, out var recipe))
            {
                continue;
            }

            AddIngredients(requirements, recipe.Ingredients, entry.Servings);
        }

        return requirements;
    }

    private static Dictionary<(Guid IngredientId, Guid UnitTypeId), decimal>
        AggregateIngredients(
            IReadOnlyCollection<RecipeIngredient> ingredients,
            int servings)
    {
        var requirements = new Dictionary<(Guid, Guid), decimal>();
        AddIngredients(requirements, ingredients, servings);
        return requirements;
    }

    private static void AddIngredients(
        Dictionary<(Guid, Guid), decimal> target,
        IReadOnlyCollection<RecipeIngredient> ingredients,
        int servings)
    {
        foreach (var ingredient in ingredients)
        {
            var key = (ingredient.IngredientId, ingredient.UnitTypeId);
            target[key] = target.GetValueOrDefault(key) + (ingredient.Quantity * servings);
        }
    }

    private void ValidateAllocations(
        IReadOnlyCollection<InventoryLotAllocationRequest> allocations,
        Dictionary<Guid, InventoryLot> lotsById,
        Dictionary<(Guid IngredientId, Guid UnitTypeId), decimal> required)
    {
        var allocatedByRequirement = new Dictionary<(Guid, Guid), decimal>();
        foreach (var allocation in allocations)
        {
            if (allocation.Quantity <= 0)
            {
                throw new InventoryConflictException(
                    "meal-completion.quantity.positive",
                    "La cantidad seleccionada debe ser positiva.");
            }

            if (!lotsById.TryGetValue(allocation.LotId, out var lot))
            {
                throw new InventoryNotFoundException(
                    "meal-completion.lot.not-found",
                    "No se encontró uno de los lotes seleccionados.");
            }

            if (lot.ExpirationDate < Today)
            {
                throw new InventoryConflictException(
                    "meal-completion.lot.expired",
                    "No se puede consumir un lote caducado.");
            }

            var key = (lot.IngredientId, lot.UnitTypeId);
            if (!required.TryGetValue(key, out var requiredQuantity))
            {
                throw new InventoryConflictException(
                    "meal-completion.lot.not-required",
                    "El lote no corresponde a una necesidad de la receta.");
            }

            var total = allocatedByRequirement.GetValueOrDefault(key) + allocation.Quantity;
            if (total > requiredQuantity)
            {
                throw new InventoryConflictException(
                    "meal-completion.quantity.exceeds-required",
                    "La cantidad seleccionada supera la necesidad de la receta.");
            }

            allocatedByRequirement[key] = total;
        }
    }

    private async Task<IReadOnlyList<InventoryRequirementResponse>> MapRequirementsAsync(
        Dictionary<(Guid IngredientId, Guid UnitTypeId), decimal> required,
        Dictionary<(Guid IngredientId, Guid UnitTypeId), decimal> available,
        CancellationToken cancellationToken)
    {
        var ingredients = (await references.ListIngredientsAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        var units = (await references.ListUnitTypesAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        return required.Select(item =>
            {
                var ingredient = ingredients[item.Key.IngredientId];
                var unit = units[item.Key.UnitTypeId];
                var availableQuantity = available.GetValueOrDefault(item.Key);
                return new InventoryRequirementResponse(
                    item.Key.IngredientId,
                    ingredient.Name.Value,
                    item.Key.UnitTypeId,
                    unit.Name.Value,
                    unit.Symbol,
                    item.Value,
                    availableQuantity,
                    Math.Max(0, item.Value - availableQuantity));
            })
            .OrderBy(item => item.IngredientName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.UnitTypeName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<MealCompletionRemainderResponse>> MapRemaindersAsync(
        Dictionary<(Guid IngredientId, Guid UnitTypeId), decimal> required,
        IReadOnlyCollection<MealLotConsumptionResponse> consumptions,
        Dictionary<Guid, InventoryLot> lotsById,
        CancellationToken cancellationToken)
    {
        var applied = consumptions
            .GroupBy(consumption =>
            {
                var lot = lotsById[consumption.LotId];
                return (lot.IngredientId, lot.UnitTypeId);
            })
            .ToDictionary(group => group.Key, group => group.Sum(item => item.AppliedQuantity));
        var ingredients = (await references.ListIngredientsAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        var units = (await references.ListUnitTypesAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        return required
            .Select(item => (item.Key, Remaining: item.Value - applied.GetValueOrDefault(item.Key)))
            .Where(item => item.Remaining > 0)
            .Select(item => new MealCompletionRemainderResponse(
                item.Key.IngredientId,
                ingredients[item.Key.IngredientId].Name.Value,
                item.Key.UnitTypeId,
                units[item.Key.UnitTypeId].Name.Value,
                units[item.Key.UnitTypeId].Symbol,
                item.Remaining))
            .OrderBy(item => item.IngredientName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.UnitTypeName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
