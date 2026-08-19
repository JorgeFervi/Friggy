using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;

namespace Friggy.Application.Inventory.Services;

/// <summary>
/// Servicio de aplicación que gestiona las operaciones sobre lotes de inventario.
/// </summary>
public sealed class InventoryLotService(
    IInventoryLotRepository lots,
    IInventoryReferenceRepository references,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Obtiene los lotes de inventario, con la posibilidad de incluir los que
    /// ya no están disponibles.
    /// </summary>
    public async Task<IReadOnlyList<InventoryLotResponse>> ListAsync(
        bool includeUnavailable,
        CancellationToken cancellationToken)
    {
        var today = Today;
        var items = await lots.ListAsync(cancellationToken);
        var selected = items
            .Where(lot => includeUnavailable ||
                (lot.Quantity > 0 && lot.ExpirationDate >= today))
            .OrderBy(lot => lot.ExpirationDate)
            .ThenBy(lot => lot.IngredientId)
            .ToArray();
        return await MapAsync(selected, cancellationToken);
    }

    /// <summary>
    /// Obtiene un lote de inventario por su identificador.
    /// </summary>
    public async Task<InventoryLotResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await MapAsync(await FindAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Crea un lote de inventario y registra sus existencias iniciales.
    /// </summary>
    public async Task<InventoryLotResponse> CreateAsync(
        CreateInventoryLotRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureReferencesAsync(
            request.IngredientId,
            request.UnitTypeId,
            cancellationToken);
        var lot = InventoryLot.Create(
            request.IngredientId,
            request.UnitTypeId,
            request.Quantity,
            request.ExpirationDate,
            timeProvider.GetUtcNow());
        await lots.AddAsync(lot, cancellationToken);
        await lots.SaveChangesAsync(cancellationToken);
        return await MapAsync(lot, cancellationToken);
    }

    /// <summary>
    /// Corrige la fecha de caducidad de un lote.
    /// </summary>
    public async Task<InventoryLotResponse> CorrectExpirationAsync(
        Guid id,
        CorrectInventoryExpirationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lot = await FindAsync(id, cancellationToken);
        lot.CorrectExpiration(request.ExpirationDate);
        await lots.SaveChangesAsync(cancellationToken);
        return await MapAsync(lot, cancellationToken);
    }

    /// <summary>
    /// Consume una cantidad de un lote de inventario.
    /// </summary>
    public Task<InventoryOperationResponse> ConsumeAsync(
        Guid id,
        InventoryQuantityRequest request,
        CancellationToken cancellationToken) =>
        ReduceAsync(id, request, InventoryMovementType.Consumption, cancellationToken);

    /// <summary>
    /// Descarta una cantidad de un lote de inventario.
    /// </summary>
    public Task<InventoryOperationResponse> DiscardAsync(
        Guid id,
        InventoryQuantityRequest request,
        CancellationToken cancellationToken) =>
        ReduceAsync(id, request, InventoryMovementType.Discard, cancellationToken);

    /// <summary>
    /// Ajusta un lote a la cantidad real disponible.
    /// </summary>
    public async Task<InventoryLotResponse> AdjustAsync(
        Guid id,
        AdjustInventoryLotRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lot = await FindAsync(id, cancellationToken);
        lot.Adjust(request.ActualQuantity, timeProvider.GetUtcNow());
        await lots.SaveChangesAsync(cancellationToken);
        return await MapAsync(lot, cancellationToken);
    }

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private async Task<InventoryOperationResponse> ReduceAsync(
        Guid id,
        InventoryQuantityRequest request,
        InventoryMovementType type,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lot = await FindAsync(id, cancellationToken);
        if (type is InventoryMovementType.Consumption && lot.ExpirationDate < Today)
        {
            throw new InventoryConflictException(
                "inventory-lot.expired",
                "No se puede consumir un lote caducado.");
        }

        var result = type is InventoryMovementType.Consumption
            ? lot.Consume(request.Quantity, timeProvider.GetUtcNow())
            : lot.Discard(request.Quantity, timeProvider.GetUtcNow());
        if (result.AppliedQuantity > 0)
        {
            await lots.SaveChangesAsync(cancellationToken);
        }

        return new InventoryOperationResponse(
            await MapAsync(lot, cancellationToken),
            result.AppliedQuantity,
            result.UnappliedQuantity);
    }

    private async Task<InventoryLot> FindAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await lots.GetByIdAsync(id, cancellationToken) ??
        throw new InventoryNotFoundException(
            "inventory-lot.not-found",
            "No se encontró el lote de inventario.");

    private async Task EnsureReferencesAsync(
        Guid ingredientId,
        Guid unitTypeId,
        CancellationToken cancellationToken)
    {
        var ingredients = await references.ListIngredientsAsync(cancellationToken);
        var units = await references.ListUnitTypesAsync(cancellationToken);
        if (!ingredients.Any(item => item.Id == ingredientId))
        {
            throw new InventoryReferenceNotFoundException(
                "inventory-lot.ingredient.not-found",
                "No se encontró el ingrediente.");
        }

        if (!units.Any(item => item.Id == unitTypeId))
        {
            throw new InventoryReferenceNotFoundException(
                "inventory-lot.unit-type.not-found",
                "No se encontró la unidad.");
        }
    }

    private async Task<InventoryLotResponse> MapAsync(
        InventoryLot lot,
        CancellationToken cancellationToken) =>
        AssertSingle(await MapAsync([lot], cancellationToken));

    private async Task<IReadOnlyList<InventoryLotResponse>> MapAsync(
        IReadOnlyCollection<InventoryLot> items,
        CancellationToken cancellationToken)
    {
        var ingredients = (await references.ListIngredientsAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        var units = (await references.ListUnitTypesAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        var today = Today;
        return items.Select(lot => Map(lot, ingredients, units, today)).ToArray();
    }

    private static InventoryLotResponse Map(
        InventoryLot lot,
        Dictionary<Guid, Ingredient> ingredients,
        Dictionary<Guid, UnitType> units,
        DateOnly today)
    {
        var ingredient = ingredients[lot.IngredientId];
        var unit = units[lot.UnitTypeId];
        return new InventoryLotResponse(
            lot.Id,
            lot.IngredientId,
            ingredient.Name.Value,
            lot.UnitTypeId,
            unit.Name.Value,
            unit.Symbol,
            lot.Quantity,
            lot.ExpirationDate,
            lot.ExpirationDate < today,
            lot.Movements
                .OrderBy(movement => movement.OccurredAt)
                .Select(movement => new InventoryMovementResponse(
                    movement.Id,
                    movement.Type switch
                    {
                        InventoryMovementType.InitialStock => InventoryMovementKind.InitialStock,
                        InventoryMovementType.Consumption => InventoryMovementKind.Consumption,
                        InventoryMovementType.Adjustment => InventoryMovementKind.Adjustment,
                        InventoryMovementType.Discard => InventoryMovementKind.Discard,
                        _ => throw new InvalidOperationException("Tipo de movimiento desconocido."),
                    },
                    movement.Delta,
                    movement.ResultingQuantity,
                    movement.OccurredAt,
                    movement.MealPlanEntryId))
                .ToArray());
    }

    private static InventoryLotResponse AssertSingle(
        IReadOnlyList<InventoryLotResponse> items) => AssertSingleCore(items);

    private static InventoryLotResponse AssertSingleCore(
        IReadOnlyList<InventoryLotResponse> items) => items.Count == 1
        ? items[0]
        : throw new InvalidOperationException("Se esperaba un lote.");
}
