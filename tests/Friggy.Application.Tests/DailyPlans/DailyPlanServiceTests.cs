using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.DailyPlans.Exceptions;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Application.DailyPlans.Services;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;

namespace Friggy.Application.Tests.DailyPlans;

public sealed class DailyPlanServiceTests
{
    [Fact]
    public async Task Create_AnyDate_AddsDefaultSlotsAndPersistsOnce()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        var service = new DailyPlanService(repository, references);
        var date = new DateOnly(2026, 8, 4);

        var result = await service.CreateAsync(
            new CreateDailyPlanRequest(date),
            TestContext.Current.CancellationToken);

        Assert.Equal(date, result.Date);
        Assert.Equal(2, result.Meals.Count);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Create_DuplicateDate_ThrowsConflictWithoutSaving()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        var date = new DateOnly(2026, 8, 4);
        repository.Items.Add(DailyPlan.Create(date));
        var service = new DailyPlanService(repository, references);

        var exception = await Assert.ThrowsAsync<DailyPlanDateConflictException>(() =>
            service.CreateAsync(
                new CreateDailyPlanRequest(date),
                TestContext.Current.CancellationToken));

        Assert.Equal("daily-plan.date.duplicate", exception.Code);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task List_InclusiveRange_ReturnsOnlyPlansInsideRangeInDateOrder()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        repository.Items.Add(DailyPlan.Create(new DateOnly(2026, 8, 6)));
        repository.Items.Add(DailyPlan.Create(new DateOnly(2026, 8, 4)));
        repository.Items.Add(DailyPlan.Create(new DateOnly(2026, 8, 3)));
        var service = new DailyPlanService(repository, references);

        var result = await service.ListAsync(
            new DateOnly(2026, 8, 4),
            new DateOnly(2026, 8, 6),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 6)],
            result.Plans.Select(plan => plan.Date));
    }

    [Fact]
    public async Task Delete_PlannedPlan_RemovesItAndPersistsOnce()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        var plan = DailyPlan.Create(new DateOnly(2026, 8, 4));
        repository.Items.Add(plan);
        var service = new DailyPlanService(repository, references);

        await service.DeleteAsync(plan.Date, TestContext.Current.CancellationToken);

        Assert.Empty(repository.Items);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Delete_CompletedPlan_ThrowsConflictWithoutSaving()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        var plan = DailyPlan.Create(new DateOnly(2026, 8, 4));
        var mealTypeId = references.MealTypes[0].Id;
        plan.Assign(mealTypeId, Guid.NewGuid());
        plan.CompleteEntry(
            mealTypeId,
            new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
        repository.Items.Add(plan);
        var service = new DailyPlanService(repository, references);

        var exception = await Assert.ThrowsAsync<DailyPlanDateConflictException>(() =>
            service.DeleteAsync(plan.Date, TestContext.Current.CancellationToken));

        Assert.Equal("daily-plan.completed.delete-conflict", exception.Code);
        Assert.Single(repository.Items);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SetEntry_ValidReferences_AssignsRecipeAndPersistsOnce()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        var plan = DailyPlan.Create(new DateOnly(2026, 8, 4));
        var mealTypeId = references.MealTypes[0].Id;
        plan.AddSlot(mealTypeId);
        repository.Items.Add(plan);
        var service = new DailyPlanService(repository, references);
        var recipeId = Guid.NewGuid();

        var result = await service.SetEntryAsync(
            plan.Date,
            mealTypeId,
            new SetMealPlanEntryRequest(recipeId, 3),
            TestContext.Current.CancellationToken);

        var meal = Assert.Single(result.Meals);
        Assert.Equal(recipeId, meal.RecipeId);
        Assert.Equal(3, meal.Servings);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task SkipEntry_ValidReason_ReturnsSkippedStateAndPersistsOnce()
    {
        var repository = new FakeDailyPlanRepository();
        var references = new FakeDailyPlanReferenceRepository();
        var plan = DailyPlan.Create(new DateOnly(2026, 8, 4));
        var mealTypeId = references.MealTypes[0].Id;
        plan.Assign(mealTypeId, Guid.NewGuid());
        repository.Items.Add(plan);
        var service = new DailyPlanService(repository, references);

        var result = await service.SkipEntryAsync(
            plan.Date,
            mealTypeId,
            new SkipMealPlanEntryRequest("  Viaje  ", "  Bocadillo  "),
            TestContext.Current.CancellationToken);

        Assert.Equal(MealPlanEntryState.Skipped, result.Status);
        Assert.Equal("Viaje", result.SkippedReason);
        Assert.Equal("Bocadillo", result.AlternativeDescription);
        Assert.Equal(1, repository.SaveCount);
    }

    private sealed class FakeDailyPlanRepository : IDailyPlanRepository
    {
        public List<DailyPlan> Items { get; } = [];

        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<DailyPlan>> ListBetweenAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DailyPlan>>(
                Items.Where(plan => plan.Date >= startDate && plan.Date <= endDate).ToArray());

        public Task<DailyPlan?> GetByDateAsync(
            DateOnly plannedDate,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(plan => plan.Date == plannedDate));

        public Task<IReadOnlyList<DateOnly>> ListExistingDatesAsync(
            IReadOnlyCollection<DateOnly> dates,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DateOnly>>(
                Items.Where(plan => dates.Contains(plan.Date)).Select(plan => plan.Date).ToArray());

        public Task AddAsync(DailyPlan plan, CancellationToken cancellationToken)
        {
            Items.Add(plan);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<DailyPlan> plans,
            CancellationToken cancellationToken)
        {
            Items.AddRange(plans);
            return Task.CompletedTask;
        }

        public void Remove(DailyPlan plan) => Items.Remove(plan);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDailyPlanReferenceRepository : IDailyPlanReferenceRepository
    {
        public MealType[] MealTypes { get; } =
        [
            MealType.Create("Desayuno", 0),
            MealType.Create("Comida", 1),
        ];

        public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<Guid, TimeSpan?>> GetRecipeEstimatedTimesAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, TimeSpan?>>(
                ids.ToDictionary(id => id, _ => (TimeSpan?)TimeSpan.FromMinutes(30)));

        public Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(MealTypes.Any(mealType => mealType.Id == id));

        public Task<IReadOnlyList<MealType>> ListMealTypesAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MealType>>(MealTypes);
    }
}
