using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Inventory;

public sealed class InventoryLot
{
    private readonly List<InventoryMovement> movements = [];

    private InventoryLot()
    {
    }

    private InventoryLot(
        Guid id,
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        DateOnly expirationDate,
        Guid version)
    {
        Id = id;
        IngredientId = ingredientId;
        UnitTypeId = unitTypeId;
        Quantity = quantity;
        ExpirationDate = expirationDate;
        Version = version;
    }

    public Guid Id { get; private set; }

    public Guid IngredientId { get; private set; }

    public Guid UnitTypeId { get; private set; }

    public decimal Quantity { get; private set; }

    public DateOnly ExpirationDate { get; private set; }

    public Guid Version { get; private set; }

    public IReadOnlyList<InventoryMovement> Movements => movements.AsReadOnly();

    public static InventoryLot Create(
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        DateOnly expirationDate,
        DateTimeOffset occurredAt)
    {
        ValidateRequiredId(ingredientId, "inventory-lot.ingredient-id.required");
        ValidateRequiredId(unitTypeId, "inventory-lot.unit-type-id.required");
        ValidatePositive(quantity, "inventory-lot.quantity.positive");

        var lot = new InventoryLot(
            Guid.NewGuid(),
            ingredientId,
            unitTypeId,
            quantity,
            expirationDate,
            Guid.NewGuid());
        lot.movements.Add(InventoryMovement.Create(
            lot.Id,
            InventoryMovementType.InitialStock,
            quantity,
            quantity,
            occurredAt));
        return lot;
    }

    public InventoryOperationResult Consume(
        decimal requestedQuantity,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId = null) =>
        Reduce(
            requestedQuantity,
            occurredAt,
            InventoryMovementType.Consumption,
            mealPlanEntryId);

    public InventoryOperationResult Discard(
        decimal requestedQuantity,
        DateTimeOffset occurredAt) =>
        Reduce(requestedQuantity, occurredAt, InventoryMovementType.Discard, null);

    public void Adjust(decimal actualQuantity, DateTimeOffset occurredAt)
    {
        if (actualQuantity < 0)
        {
            throw new DomainValidationException(
                "inventory-lot.adjustment.non-negative",
                "La cantidad real no puede ser negativa.");
        }

        var delta = actualQuantity - Quantity;
        if (delta == 0)
        {
            return;
        }

        Quantity = actualQuantity;
        RegisterMovement(InventoryMovementType.Adjustment, delta, occurredAt, null);
    }

    public void CorrectExpiration(DateOnly expirationDate)
    {
        ExpirationDate = expirationDate;
        Version = Guid.NewGuid();
    }

    private InventoryOperationResult Reduce(
        decimal requestedQuantity,
        DateTimeOffset occurredAt,
        InventoryMovementType type,
        Guid? mealPlanEntryId)
    {
        ValidatePositive(requestedQuantity, "inventory-lot.operation.quantity.positive");

        var applied = Math.Min(Quantity, requestedQuantity);
        var unapplied = requestedQuantity - applied;
        if (applied > 0)
        {
            Quantity -= applied;
            RegisterMovement(type, -applied, occurredAt, mealPlanEntryId);
        }

        return new InventoryOperationResult(applied, unapplied);
    }

    private void RegisterMovement(
        InventoryMovementType type,
        decimal delta,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId)
    {
        movements.Add(InventoryMovement.Create(
            Id,
            type,
            delta,
            Quantity,
            occurredAt,
            mealPlanEntryId));
        Version = Guid.NewGuid();
    }

    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }

    private static void ValidatePositive(decimal quantity, string code)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException(code, "La cantidad debe ser positiva.");
        }
    }
}
