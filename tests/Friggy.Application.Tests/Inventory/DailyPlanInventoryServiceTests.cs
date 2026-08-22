using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Application.Inventory.Services;
using Friggy.Application.Recipes.Dtos;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;
using Friggy.Domain.Inventory;
using Friggy.Domain.Recipes;

namespace Friggy.Application.Tests.Inventory;

public sealed class DailyPlanInventoryServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetRequirements_RepeatedLinesAndServings_AggregatesAndCalculatesShortage()
    {
        var scenario = Scenario.Create(servings: 3);
        scenario.Recipe.AddIngredient(
            scenario.Ingredient.Id,
            scenario.Unit.Id,
            0.5m,
            1);
        scenario.AddLot(2m, new DateOnly(2026, 8, 20));
        scenario.AddLot(100m, new DateOnly(2026, 8, 11));

        var result = await scenario.Service.GetRequirementsAsync(
            scenario.Plan.Date,
            TestContext.Current.CancellationToken);

        var requirement = Assert.Single(result);
        Assert.Equal(4.5m, requirement.RequiredQuantity);
        Assert.Equal(2m, requirement.AvailableQuantity);
        Assert.Equal(2.5m, requirement.MissingQuantity);
    }

    [Fact]
    public async Task GetRequirements_AssigningIngredientLinesToStep_DoesNotChangeRequiredQuantities()
    {
        var scenario = Scenario.Create(servings: 2);
        var secondLine = scenario.Recipe.AddIngredient(
            scenario.Ingredient.Id,
            scenario.Unit.Id,
            0.5m,
            1);

        var beforeAssociations = await scenario.Service.GetRequirementsAsync(
            scenario.Plan.Date,
            TestContext.Current.CancellationToken);

        var step = Assert.Single(scenario.Recipe.Steps);
        scenario.Recipe.AssignIngredientToStep(
            step.Id,
            scenario.Recipe.Ingredients[0].Id);
        scenario.Recipe.AssignIngredientToStep(step.Id, secondLine.Id);

        var afterAssociations = await scenario.Service.GetRequirementsAsync(
            scenario.Plan.Date,
            TestContext.Current.CancellationToken);

        Assert.Equal(beforeAssociations.ToArray(), afterAssociations.ToArray());
        Assert.Equal(3m, Assert.Single(afterAssociations).RequiredQuantity);
    }

    [Fact]
    public async Task GetRequirements_ExpirationBoundary_ExcludesYesterdayAndIncludesTodayAndTomorrow()
    {
        var scenario = Scenario.Create(servings: 1);
        scenario.AddLot(10m, new DateOnly(2026, 8, 11));
        scenario.AddLot(0.25m, new DateOnly(2026, 8, 12));
        scenario.AddLot(0.5m, new DateOnly(2026, 8, 13));

        var result = await scenario.Service.GetRequirementsAsync(
            scenario.Plan.Date,
            TestContext.Current.CancellationToken);

        var requirement = Assert.Single(result);
        Assert.Equal(0.75m, requirement.AvailableQuantity);
        Assert.Equal(0.25m, requirement.MissingQuantity);
    }

    [Fact]
    public async Task GetRequirements_CancelledRequest_PropagatesCancellation()
    {
        var scenario = Scenario.Create(servings: 1);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.Service.GetRequirementsAsync(scenario.Plan.Date, cancellation.Token));
    }

    [Fact]
    public async Task CompleteMeal_SelectedLots_ConsumesAtomicallyAndLinksMovements()
    {
        var scenario = Scenario.Create(servings: 2);
        var first = scenario.AddLot(1m, new DateOnly(2026, 8, 13));
        var second = scenario.AddLot(2m, new DateOnly(2026, 8, 20));

        var result = await scenario.Service.CompleteMealAsync(
            scenario.Plan.Date,
            scenario.MealType.Id,
            new CompleteMealRequest(
                [new(first.Id, 1m), new(second.Id, 1m)]),
            TestContext.Current.CancellationToken);

        Assert.False(result.AlreadyCompleted);
        Assert.True(Assert.Single(scenario.Plan.Entries).IsCompleted);
        Assert.Equal(0m, first.Quantity);
        Assert.Equal(1m, second.Quantity);
        Assert.All(
            new[] { first.Movements[^1], second.Movements[^1] },
            movement => Assert.Equal(result.MealPlanEntryId, movement.MealPlanEntryId));
        Assert.Equal(1, scenario.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CompleteMeal_PartialStock_ReportsRemainingRequirement()
    {
        var scenario = Scenario.Create(servings: 1);
        var lot = scenario.AddLot(0.25m, new DateOnly(2026, 8, 20));

        var result = await scenario.Service.CompleteMealAsync(
            scenario.Plan.Date,
            scenario.MealType.Id,
            new CompleteMealRequest([new(lot.Id, 1m)]),
            TestContext.Current.CancellationToken);

        var consumption = Assert.Single(result.Consumptions);
        Assert.Equal(0.25m, consumption.AppliedQuantity);
        Assert.Equal(0.75m, consumption.UnappliedQuantity);
        var remaining = Assert.Single(result.RemainingRequirements ?? []);
        Assert.Equal(scenario.Ingredient.Id, remaining.IngredientId);
        Assert.Equal(0.75m, remaining.RemainingQuantity);
    }

    [Fact]
    public async Task CompleteMeal_RepeatedRequest_DoesNotDuplicateConsumption()
    {
        var scenario = Scenario.Create(servings: 1);
        var lot = scenario.AddLot(2m, new DateOnly(2026, 8, 20));
        var request = new CompleteMealRequest([new(lot.Id, 1m)]);
        await scenario.Service.CompleteMealAsync(
            scenario.Plan.Date,
            scenario.MealType.Id,
            request,
            TestContext.Current.CancellationToken);

        var retry = await scenario.Service.CompleteMealAsync(
            scenario.Plan.Date,
            scenario.MealType.Id,
            request,
            TestContext.Current.CancellationToken);

        Assert.True(retry.AlreadyCompleted);
        Assert.Equal(1m, lot.Quantity);
        Assert.Equal(2, lot.Movements.Count);
        Assert.Equal(1, scenario.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CompleteMeal_LotWithDifferentIngredient_RejectsBeforeMutation()
    {
        var scenario = Scenario.Create(servings: 1);
        var otherIngredient = Ingredient.Create("Otro");
        scenario.References.Ingredients.Add(otherIngredient);
        var lot = InventoryLot.Create(
            otherIngredient.Id,
            scenario.Unit.Id,
            1m,
            new DateOnly(2026, 8, 20),
            Now);
        scenario.Lots.Items.Add(lot);

        var exception = await Assert.ThrowsAsync<InventoryConflictException>(() =>
            scenario.Service.CompleteMealAsync(
                scenario.Plan.Date,
                scenario.MealType.Id,
                new CompleteMealRequest([new(lot.Id, 1m)]),
                TestContext.Current.CancellationToken));

        Assert.Equal("meal-completion.lot.not-required", exception.Code);
        Assert.False(Assert.Single(scenario.Plan.Entries).IsCompleted);
        Assert.Equal(1m, lot.Quantity);
        Assert.Equal(0, scenario.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetRequirements_SkippedMeal_DoesNotRequireIngredients()
    {
        var scenario = Scenario.Create(servings: 2);
        scenario.Plan.SkipEntry(
            scenario.MealType.Id,
            "Comida fuera de casa",
            null);

        var result = await scenario.Service.GetRequirementsAsync(
            scenario.Plan.Date,
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CompleteMeal_SkippedMeal_RejectsBeforeInventoryMutation()
    {
        var scenario = Scenario.Create(servings: 1);
        var lot = scenario.AddLot(2m, new DateOnly(2026, 8, 20));
        scenario.Plan.SkipEntry(
            scenario.MealType.Id,
            "Sin hambre",
            "Fruta");

        var exception = await Assert.ThrowsAsync<InventoryConflictException>(() =>
            scenario.Service.CompleteMealAsync(
                scenario.Plan.Date,
                scenario.MealType.Id,
                new CompleteMealRequest([new(lot.Id, 1m)]),
                TestContext.Current.CancellationToken));

        Assert.Equal("meal-completion.entry.skipped", exception.Code);
        Assert.Equal(2m, lot.Quantity);
        Assert.Single(lot.Movements);
        Assert.True(Assert.Single(scenario.Plan.Entries).IsSkipped);
        Assert.Equal(0, scenario.UnitOfWork.SaveCount);
    }

    private sealed class Scenario
    {
        private Scenario(int servings)
        {
            Ingredient = Ingredient.Create("Tomate");
            Unit = UnitType.Create("Kilogramo", "kg");
            MealType = MealType.Create("Comida", 1);
            Recipe = Recipe.Create("Ensalada", TimeSpan.Zero);
            Recipe.AddIngredient(Ingredient.Id, Unit.Id, 1m, 0);
            Recipe.AddStep("Servir", null, 0);
            Plan = DailyPlan.Create(new DateOnly(2026, 8, 10));
            Plan.Assign(MealType.Id, Recipe.Id, servings);
            Plans.Items.Add(Plan);
            Recipes.Items.Add(Recipe);
            References.Ingredients.Add(Ingredient);
            References.UnitTypes.Add(Unit);
            Service = new DailyPlanInventoryService(
                Plans,
                Recipes,
                Lots,
                References,
                UnitOfWork,
                new FixedTimeProvider(Now));
        }

        public FakeDailyPlanRepository Plans { get; } = new();
        public FakeRecipeRepository Recipes { get; } = new();
        public FakeInventoryLotRepository Lots { get; } = new();
        public FakeInventoryReferenceRepository References { get; } = new();
        public FakeInventoryUnitOfWork UnitOfWork { get; } = new();
        public Ingredient Ingredient { get; }
        public UnitType Unit { get; }
        public MealType MealType { get; }
        public Recipe Recipe { get; }
        public DailyPlan Plan { get; }
        public DailyPlanInventoryService Service { get; }

        public static Scenario Create(int servings) => new(servings);

        public InventoryLot AddLot(decimal quantity, DateOnly expirationDate)
        {
            var lot = InventoryLot.Create(
                Ingredient.Id,
                Unit.Id,
                quantity,
                expirationDate,
                Now.AddHours(-1));
            Lots.Items.Add(lot);
            return lot;
        }
    }

    private sealed class FakeInventoryUnitOfWork : IInventoryUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryLotRepository : IInventoryLotRepository
    {
        public List<InventoryLot> Items { get; } = [];
        public Task<IReadOnlyList<InventoryLot>> ListAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<IReadOnlyList<InventoryLot>>(cancellationToken)
                : Task.FromResult<IReadOnlyList<InventoryLot>>(Items);
        public Task<IReadOnlyList<InventoryLot>> ListForUpdateAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InventoryLot>>(Items);
        public Task<InventoryLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeInventoryReferenceRepository : IInventoryReferenceRepository
    {
        public List<Ingredient> Ingredients { get; } = [];
        public List<UnitType> UnitTypes { get; } = [];
        public Task<IReadOnlyList<Ingredient>> ListIngredientsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Ingredient>>(Ingredients);
        public Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnitType>>(UnitTypes);
    }

    private sealed class FakeDailyPlanRepository : IDailyPlanRepository
    {
        public List<DailyPlan> Items { get; } = [];
        public Task<IReadOnlyList<DailyPlan>> ListBetweenAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DailyPlan>>(
                Items.Where(item => item.Date >= startDate && item.Date <= endDate).ToArray());
        public Task<DailyPlan?> GetByDateAsync(
            DateOnly plannedDate,
            CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<DailyPlan?>(cancellationToken)
                : Task.FromResult(Items.SingleOrDefault(item => item.Date == plannedDate));
        public Task AddAsync(DailyPlan plan, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public void Remove(DailyPlan plan) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        public List<Recipe> Items { get; } = [];
        public Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Recipe>>(Items);
        public Task<IReadOnlyList<RecipeListItemResponse>> ListSummariesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public void Remove(Recipe recipe) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
