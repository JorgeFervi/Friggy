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
