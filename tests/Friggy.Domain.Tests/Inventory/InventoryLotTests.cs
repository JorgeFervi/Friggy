using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;

namespace Friggy.Domain.Tests.Inventory;

public sealed class InventoryLotTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 12, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidLot_StoresQuantityAndInitialMovement()
    {
        var ingredientId = Guid.NewGuid();
        var unitTypeId = Guid.NewGuid();

        var lot = InventoryLot.Create(
            ingredientId,
            unitTypeId,
            2.5m,
            new DateOnly(2026, 8, 20),
            OccurredAt);

        Assert.Equal(ingredientId, lot.IngredientId);
        Assert.Equal(unitTypeId, lot.UnitTypeId);
        Assert.Equal(2.5m, lot.Quantity);
        var movement = Assert.Single(lot.Movements);
        Assert.Equal(InventoryMovementType.InitialStock, movement.Type);
        Assert.Equal(2.5m, movement.Delta);
        Assert.Equal(2.5m, movement.ResultingQuantity);
        Assert.Equal(OccurredAt, movement.OccurredAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    public void Create_NonPositiveQuantity_Throws(decimal quantity)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            InventoryLot.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                quantity,
                new DateOnly(2026, 8, 20),
                OccurredAt));

        Assert.Equal("inventory-lot.quantity.positive", exception.Code);
    }

    [Fact]
    public void Consume_LessThanAvailable_ChangesQuantityAndAddsMovement()
    {
        var lot = CreateLot(5m);
        var mealPlanEntryId = Guid.NewGuid();

        var result = lot.Consume(2m, OccurredAt.AddMinutes(5), mealPlanEntryId);

        Assert.Equal(2m, result.AppliedQuantity);
        Assert.Equal(0m, result.UnappliedQuantity);
        Assert.Equal(3m, lot.Quantity);
        var movement = lot.Movements[^1];
        Assert.Equal(InventoryMovementType.Consumption, movement.Type);
        Assert.Equal(-2m, movement.Delta);
        Assert.Equal(3m, movement.ResultingQuantity);
        Assert.Equal(mealPlanEntryId, movement.MealPlanEntryId);
    }

    [Fact]
    public void Consume_MoreThanAvailable_AppliesAvailableAndReportsRemainder()
    {
        var lot = CreateLot(1.5m);

        var result = lot.Consume(2m, OccurredAt.AddMinutes(5));

        Assert.Equal(1.5m, result.AppliedQuantity);
        Assert.Equal(0.5m, result.UnappliedQuantity);
        Assert.Equal(0m, lot.Quantity);
        Assert.Equal(-1.5m, lot.Movements[^1].Delta);
    }

    [Fact]
    public void Discard_MoreThanAvailable_NeverProducesNegativeStock()
    {
        var lot = CreateLot(1m);

        var result = lot.Discard(4m, OccurredAt.AddMinutes(5));

        Assert.Equal(1m, result.AppliedQuantity);
        Assert.Equal(3m, result.UnappliedQuantity);
        Assert.Equal(0m, lot.Quantity);
        Assert.Equal(InventoryMovementType.Discard, lot.Movements[^1].Type);
    }

    [Fact]
    public void Adjust_ActualQuantity_RecordsCalculatedDifference()
    {
        var lot = CreateLot(5m);

        lot.Adjust(3.25m, OccurredAt.AddMinutes(5));

        Assert.Equal(3.25m, lot.Quantity);
        var movement = lot.Movements[^1];
        Assert.Equal(InventoryMovementType.Adjustment, movement.Type);
        Assert.Equal(-1.75m, movement.Delta);
        Assert.Equal(3.25m, movement.ResultingQuantity);
    }

    [Fact]
    public void Adjust_SameQuantity_DoesNotCreateMovement()
    {
        var lot = CreateLot(5m);

        lot.Adjust(5m, OccurredAt.AddMinutes(5));

        Assert.Single(lot.Movements);
    }

    [Fact]
    public void Movements_CannotBeMutatedOutsideAggregate()
    {
        var lot = CreateLot(5m);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<InventoryMovement>)lot.Movements).Clear());
        Assert.Single(lot.Movements);
    }

    private static InventoryLot CreateLot(decimal quantity) =>
        InventoryLot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity,
            new DateOnly(2026, 8, 20),
            OccurredAt);
}
