using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Application.Inventory.Services;
using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;

namespace Friggy.Application.Tests.Inventory;

public sealed class InventoryLotServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_ValidRequest_PersistsLotWithInitialMovement()
    {
        var scenario = InventoryScenario.Create();

        var result = await scenario.Service.CreateAsync(
            new CreateInventoryLotRequest(
                scenario.Ingredient.Id,
                scenario.UnitType.Id,
                2.5m,
                new DateOnly(2026, 8, 20)),
            TestContext.Current.CancellationToken);

        Assert.Equal(2.5m, result.Quantity);
        Assert.Equal("Tomate", result.IngredientName);
        Assert.Equal("kg", result.UnitSymbol);
        Assert.Equal(1, scenario.Repository.SaveCount);
        Assert.Single(Assert.Single(scenario.Repository.Items).Movements);
    }

    [Fact]
    public async Task Create_MissingReference_ThrowsAndDoesNotPersist()
    {
        var scenario = InventoryScenario.Create();

        var exception = await Assert.ThrowsAsync<InventoryReferenceNotFoundException>(() =>
            scenario.Service.CreateAsync(
                new CreateInventoryLotRequest(
                    Guid.NewGuid(),
                    scenario.UnitType.Id,
                    1m,
                    new DateOnly(2026, 8, 20)),
                TestContext.Current.CancellationToken));

        Assert.Equal("inventory-lot.ingredient.not-found", exception.Code);
        Assert.Empty(scenario.Repository.Items);
        Assert.Equal(0, scenario.Repository.SaveCount);
    }

    [Fact]
    public async Task ListAvailable_ExcludesExpiredAndExhaustedButIncludesExpiringToday()
    {
        var scenario = InventoryScenario.Create();
        scenario.AddLot(1m, new DateOnly(2026, 8, 11));
        scenario.AddLot(1m, new DateOnly(2026, 8, 12));
        scenario.AddLot(1m, new DateOnly(2026, 8, 13)).Adjust(0m, Now);

        var result = await scenario.Service.ListAsync(
            includeUnavailable: false,
            TestContext.Current.CancellationToken);

        var lot = Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 8, 12), lot.ExpirationDate);
    }

    [Fact]
    public async Task Consume_MoreThanAvailable_ReturnsAppliedAndUnappliedQuantities()
    {
        var scenario = InventoryScenario.Create();
        var lot = scenario.AddLot(1.5m, new DateOnly(2026, 8, 20));

        var result = await scenario.Service.ConsumeAsync(
            lot.Id,
            new InventoryQuantityRequest(2m),
            TestContext.Current.CancellationToken);

        Assert.Equal(1.5m, result.AppliedQuantity);
        Assert.Equal(0.5m, result.UnappliedQuantity);
        Assert.Equal(0m, result.Lot.Quantity);
        Assert.Equal(1, scenario.Repository.SaveCount);
    }

    [Fact]
    public async Task Consume_ExpiredLot_ThrowsAndDoesNotSave()
    {
        var scenario = InventoryScenario.Create();
        var lot = scenario.AddLot(1m, new DateOnly(2026, 8, 11));

        var exception = await Assert.ThrowsAsync<InventoryConflictException>(() =>
            scenario.Service.ConsumeAsync(
                lot.Id,
                new InventoryQuantityRequest(1m),
                TestContext.Current.CancellationToken));

        Assert.Equal("inventory-lot.expired", exception.Code);
        Assert.Equal(1m, lot.Quantity);
        Assert.Equal(0, scenario.Repository.SaveCount);
    }

    private sealed class InventoryScenario
    {
        private InventoryScenario(
            FakeInventoryLotRepository repository,
            FakeInventoryReferenceRepository references,
            Ingredient ingredient,
            UnitType unitType)
        {
            Repository = repository;
            References = references;
            Ingredient = ingredient;
            UnitType = unitType;
            Service = new InventoryLotService(repository, references, new FixedTimeProvider(Now));
        }

        public FakeInventoryLotRepository Repository { get; }

        public FakeInventoryReferenceRepository References { get; }

        public Ingredient Ingredient { get; }

        public UnitType UnitType { get; }

        public InventoryLotService Service { get; }

        public static InventoryScenario Create()
        {
            var ingredient = Ingredient.Create("Tomate");
            var unitType = UnitType.Create("Kilogramo", "kg");
            var references = new FakeInventoryReferenceRepository();
            references.Ingredients.Add(ingredient);
            references.UnitTypes.Add(unitType);
            return new(new FakeInventoryLotRepository(), references, ingredient, unitType);
        }

        public InventoryLot AddLot(decimal quantity, DateOnly expirationDate)
        {
            var lot = InventoryLot.Create(
                Ingredient.Id,
                UnitType.Id,
                quantity,
                expirationDate,
                Now.AddHours(-1));
            Repository.Items.Add(lot);
            return lot;
        }
    }

    private sealed class FakeInventoryLotRepository : IInventoryLotRepository
    {
        public List<InventoryLot> Items { get; } = [];

        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<InventoryLot>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<InventoryLot>>(Items);
        }

        public Task<InventoryLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<InventoryLot>> ListForUpdateAsync(
            CancellationToken cancellationToken) => ListAsync(cancellationToken);

        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(lot);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryReferenceRepository : IInventoryReferenceRepository
    {
        public List<Ingredient> Ingredients { get; } = [];

        public List<UnitType> UnitTypes { get; } = [];

        public Task<IReadOnlyList<Ingredient>> ListIngredientsAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Ingredient>>(Ingredients);
        }

        public Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<UnitType>>(UnitTypes);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
