using Friggy.Application.ShoppingLists.Dtos;
using Friggy.Application.ShoppingLists.Interfaces;
using Friggy.Application.ShoppingLists.Services;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Tests.ShoppingLists;

public sealed class ShoppingListServiceTests
{
    [Fact]
    public async Task GetAsync_RepeatedRequirementAndPartialStock_ReturnsAggregatedPurchase()
    {
        var ingredientId = Guid.NewGuid(); var unitId = Guid.NewGuid();
        var repository = new FakeRepository(new([new(ingredientId, "Tomate", unitId, "Gramo", "g", 750m)], [new(ingredientId, unitId, 200m)]));
        var result = await new ShoppingListService(repository, new FixedTimeProvider()).GetAsync(new(2030, 1, 6), new(2030, 1, 8), TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Items);
        Assert.Equal((750m, 200m, 550m), (item.RequiredQuantity, item.AvailableQuantity, item.QuantityToBuy));
    }

    [Fact]
    public async Task GetAsync_SameIngredientInDifferentUnits_KeepsSeparateLinesAndCoveredStock()
    {
        var ingredientId = Guid.NewGuid(); var grams = Guid.NewGuid(); var units = Guid.NewGuid();
        var repository = new FakeRepository(new([
            new(ingredientId, "Tomate", grams, "Gramo", "g", 500m),
            new(ingredientId, "Tomate", units, "Unidad", "ud", 3m)],
            [new(ingredientId, grams, 600m), new(ingredientId, units, 1m)]));
        var result = await new ShoppingListService(repository, new FixedTimeProvider()).GetAsync(new(2030, 1, 1), new(2030, 1, 1), TestContext.Current.CancellationToken);
        Assert.Collection(result.Items, gram => Assert.Equal(0m, gram.QuantityToBuy), unit => Assert.Equal(2m, unit.QuantityToBuy));
    }

    [Fact]
    public async Task GetAsync_TablespoonDemandAndLiterStock_ReturnsMilliliters()
    {
        var ingredientId = Guid.NewGuid();
        var milliliter = UnitType.Create(
            "Mililitro", "ml", MeasurementDimension.Volume, 1m, true, true);
        var liter = UnitType.Create(
            "Litro", "l", MeasurementDimension.Volume, 1000m, true, true);
        var tablespoon = UnitType.Create(
            "Cucharada", "cda", MeasurementDimension.Volume, 15m, true, false);
        var snapshot = new ShoppingListSnapshot(
            [new(ingredientId, "Aceite", tablespoon.Id, tablespoon.Name.Value, tablespoon.Symbol, 2m)],
            [new(ingredientId, liter.Id, 0.01m)])
        {
            Units = [milliliter, liter, tablespoon],
        };

        var result = await new ShoppingListService(
            new FakeRepository(snapshot),
            new FixedTimeProvider()).GetAsync(
                new(2030, 1, 1),
                new(2030, 1, 1),
                TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal(milliliter.Id, item.UnitTypeId);
        Assert.Equal((30m, 10m, 20m),
            (item.RequiredQuantity, item.AvailableQuantity, item.QuantityToBuy));
    }

    [Fact]
    public async Task GetAsync_LargeVolumeDemand_SelectsLiterDeterministically()
    {
        var ingredientId = Guid.NewGuid();
        var milliliter = UnitType.Create(
            "Mililitro", "ml", MeasurementDimension.Volume, 1m, true, true);
        var liter = UnitType.Create(
            "Litro", "l", MeasurementDimension.Volume, 1000m, true, true);
        var snapshot = new ShoppingListSnapshot(
            [new(ingredientId, "Aceite", milliliter.Id, "Mililitro", "ml", 1500m)],
            [])
        {
            Units = [milliliter, liter],
        };

        var result = await new ShoppingListService(
            new FakeRepository(snapshot),
            new FixedTimeProvider()).GetAsync(
                new(2030, 1, 1),
                new(2030, 1, 1),
                TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal(liter.Id, item.UnitTypeId);
        Assert.Equal(1.5m, item.RequiredQuantity);
    }

    [Fact]
    public async Task GetAsync_DimensionWithoutShoppingUnit_ThrowsStableValidation()
    {
        var ingredientId = Guid.NewGuid();
        var tablespoon = UnitType.Create(
            "Cucharada", "cda", MeasurementDimension.Volume, 15m, true, false);
        var snapshot = new ShoppingListSnapshot(
            [new(ingredientId, "Aceite", tablespoon.Id, "Cucharada", "cda", 1m)],
            [])
        {
            Units = [tablespoon],
        };

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            new ShoppingListService(
                new FakeRepository(snapshot),
                new FixedTimeProvider()).GetAsync(
                    new(2030, 1, 1),
                    new(2030, 1, 1),
                    TestContext.Current.CancellationToken));

        Assert.Equal("unit-type.shopping-unit.required", exception.Code);
    }

    [Fact]
    public async Task GetAsync_InvertedRange_ThrowsWithoutQueryingRepository()
    {
        var repository = new FakeRepository(new([], []));
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => new ShoppingListService(repository, new FixedTimeProvider()).GetAsync(new(2030, 1, 2), new(2030, 1, 1), TestContext.Current.CancellationToken));
        Assert.Equal("shopping-list.date-range.invalid", exception.Code); Assert.False(repository.WasCalled);
    }

    private sealed class FakeRepository(ShoppingListSnapshot snapshot) : IShoppingListReadRepository
    {
        public bool WasCalled { get; private set; }
        public Task<ShoppingListSnapshot> GetSnapshotAsync(DateOnly from, DateOnly endDate, DateOnly today, CancellationToken cancellationToken) { WasCalled = true; return Task.FromResult(snapshot); }
    }
    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
