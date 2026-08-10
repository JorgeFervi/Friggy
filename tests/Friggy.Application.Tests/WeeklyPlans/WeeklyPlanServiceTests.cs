using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Application.WeeklyPlans.Exceptions;
using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Application.WeeklyPlans.Services;
using Friggy.Domain.Catalogs;
using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.Tests.WeeklyPlans;

public sealed class WeeklyPlanServiceTests
{
    [Fact]
    public async Task Create_ValidRequest_PersistsAndReturnsEmptySevenDayCalendar()
    {
        var scenario = WeeklyPlanScenario.Create();
        var service = scenario.CreateService();

        var result = await service.CreateAsync(
            new CreateWeeklyPlanRequest(
                "  Semana 32  ",
                new DateOnly(2026, 8, 3),
                "  Vacaciones  "),
            TestContext.Current.CancellationToken);

        Assert.Equal(result.Id, Assert.Single(scenario.Plans.Items).Id);
        Assert.Equal("Semana 32", result.Name);
        Assert.Equal("Vacaciones", result.Description);
        Assert.Equal(1, scenario.Plans.SaveCount);
        Assert.Equal(
            Enumerable.Range(0, 7).Select(result.StartDate.AddDays),
            result.Days.Select(day => day.Date));
        Assert.All(result.Days, day =>
        {
            Assert.Equal([0, 1, 2], day.Meals.Select(meal => meal.MealTypeOrder));
            Assert.All(day.Meals, meal => Assert.Null(meal.RecipeId));
        });
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsAndDoesNotPersist()
    {
        var scenario = WeeklyPlanScenario.Create();
        scenario.AddPlan("Semana 32", new DateOnly(2026, 7, 27));
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<WeeklyPlanNameConflictException>(() =>
            service.CreateAsync(
                new CreateWeeklyPlanRequest(
                    " semana 32 ",
                    new DateOnly(2026, 8, 3),
                    null),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.name.duplicate", exception.Code);
        Assert.Single(scenario.Plans.Items);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task List_UnorderedPlans_ReturnsPlansOrderedByStartDateThenName()
    {
        var scenario = WeeklyPlanScenario.Create();
        scenario.AddPlan("Semana B", new DateOnly(2026, 8, 10));
        scenario.AddPlan("Semana C", new DateOnly(2026, 8, 3));
        scenario.AddPlan("Semana A", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        var result = await service.ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["Semana A", "Semana C", "Semana B"],
            result.Select(plan => plan.Name));
    }

    [Fact]
    public async Task Get_MissingPlan_ThrowsNotFound()
    {
        var scenario = WeeklyPlanScenario.Create();
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<WeeklyPlanNotFoundException>(() =>
            service.GetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.not-found", exception.Code);
    }

    [Fact]
    public async Task Get_ExistingPlan_ReturnsSevenDaysAndOrderedMealTypes()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        plan.Assign(plan.StartDate.AddDays(2), scenario.LunchId, scenario.RecipeId);
        var service = scenario.CreateService();

        var result = await service.GetAsync(
            plan.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(7, result.Days.Count);
        Assert.All(result.Days, day =>
            Assert.Equal([0, 1, 2], day.Meals.Select(meal => meal.MealTypeOrder)));
        var assignedDay = result.Days.Single(day => day.Date == plan.StartDate.AddDays(2));
        Assert.Equal(
            scenario.RecipeId,
            assignedDay.Meals.Single(meal => meal.MealTypeId == scenario.LunchId).RecipeId);
    }

    [Fact]
    public async Task Update_ValidRequest_ChangesDetailsAndPreservesSchedule()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Anterior", new DateOnly(2026, 8, 3));
        plan.Assign(plan.StartDate, scenario.BreakfastId, scenario.RecipeId);
        var entryId = Assert.Single(plan.Entries).Id;
        var service = scenario.CreateService();

        var result = await service.UpdateAsync(
            plan.Id,
            new UpdateWeeklyPlanRequest("  Vacaciones  ", "  Costa  "),
            TestContext.Current.CancellationToken);

        Assert.Equal(plan.Id, result.Id);
        Assert.Equal("Vacaciones", result.Name);
        Assert.Equal("Costa", result.Description);
        Assert.Equal(entryId, Assert.Single(plan.Entries).Id);
        Assert.Equal(1, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task Update_DuplicateName_ThrowsAndPreservesExistingPlan()
    {
        var scenario = WeeklyPlanScenario.Create();
        var existing = scenario.AddPlan("Original", new DateOnly(2026, 8, 3));
        scenario.AddPlan("Vacaciones", new DateOnly(2026, 8, 10));
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<WeeklyPlanNameConflictException>(() =>
            service.UpdateAsync(
                existing.Id,
                new UpdateWeeklyPlanRequest(" vacaciones ", "Costa"),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.name.duplicate", exception.Code);
        Assert.Equal("Original", existing.Name.Value);
        Assert.Null(existing.Description);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task Delete_ExistingPlan_RemovesAndSaves()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        await service.DeleteAsync(plan.Id, TestContext.Current.CancellationToken);

        Assert.Empty(scenario.Plans.Items);
        Assert.Equal(1, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task Delete_MissingPlan_ThrowsAndDoesNotSave()
    {
        var scenario = WeeklyPlanScenario.Create();
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<WeeklyPlanNotFoundException>(() =>
            service.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.not-found", exception.Code);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SetEntry_ValidReferences_AssignsRecipeAndSaves()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        var result = await service.SetEntryAsync(
            plan.Id,
            plan.StartDate.AddDays(1),
            scenario.BreakfastId,
            new SetMealPlanEntryRequest(scenario.RecipeId),
            TestContext.Current.CancellationToken);

        Assert.Equal(scenario.RecipeId, Assert.Single(plan.Entries).RecipeId);
        Assert.Equal(1, scenario.Plans.SaveCount);
        var assignedDay = result.Days.Single(day => day.Date == plan.StartDate.AddDays(1));
        Assert.Equal(
            scenario.RecipeId,
            assignedDay.Meals.Single(meal => meal.MealTypeId == scenario.BreakfastId).RecipeId);
    }

    [Fact]
    public async Task SetEntry_MissingRecipe_ThrowsAndDoesNotMutate()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<WeeklyPlanReferenceNotFoundException>(() =>
            service.SetEntryAsync(
                plan.Id,
                plan.StartDate,
                scenario.BreakfastId,
                new SetMealPlanEntryRequest(Guid.NewGuid()),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.recipe.not-found", exception.Code);
        Assert.Empty(plan.Entries);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SetEntry_MissingMealType_ThrowsAndDoesNotMutate()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<WeeklyPlanReferenceNotFoundException>(() =>
            service.SetEntryAsync(
                plan.Id,
                plan.StartDate,
                Guid.NewGuid(),
                new SetMealPlanEntryRequest(scenario.RecipeId),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.meal-type.not-found", exception.Code);
        Assert.Empty(plan.Entries);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SetEntry_DateOutsideWeek_ThrowsAndDoesNotMutate()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.SetEntryAsync(
                plan.Id,
                plan.StartDate.AddDays(7),
                scenario.BreakfastId,
                new SetMealPlanEntryRequest(scenario.RecipeId),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.entry.date.out-of-range", exception.Code);
        Assert.Empty(plan.Entries);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SetEntry_CancelledToken_PropagatesCancellationAndDoesNotMutate()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = scenario.CreateService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SetEntryAsync(
                plan.Id,
                plan.StartDate,
                scenario.BreakfastId,
                new SetMealPlanEntryRequest(scenario.RecipeId),
                cancellation.Token));

        Assert.Empty(plan.Entries);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task RemoveEntry_ExistingSlot_ClearsCellAndSaves()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        plan.Assign(plan.StartDate, scenario.BreakfastId, scenario.RecipeId);
        var service = scenario.CreateService();

        var result = await service.RemoveEntryAsync(
            plan.Id,
            plan.StartDate,
            scenario.BreakfastId,
            TestContext.Current.CancellationToken);

        Assert.Empty(plan.Entries);
        Assert.Equal(1, scenario.Plans.SaveCount);
        Assert.Null(result.Days[0].Meals[0].RecipeId);
    }

    [Fact]
    public async Task RemoveEntry_MissingSlot_IsIdempotentAndDoesNotSave()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var service = scenario.CreateService();

        var result = await service.RemoveEntryAsync(
            plan.Id,
            plan.StartDate,
            scenario.BreakfastId,
            TestContext.Current.CancellationToken);

        Assert.Empty(plan.Entries);
        Assert.Equal(0, scenario.Plans.SaveCount);
        Assert.Null(result.Days[0].Meals[0].RecipeId);
    }

    private sealed class WeeklyPlanScenario
    {
        private WeeklyPlanScenario(
            FakeWeeklyPlanRepository plans,
            FakeWeeklyPlanReferenceRepository references,
            Guid recipeId,
            Guid breakfastId,
            Guid lunchId)
        {
            Plans = plans;
            References = references;
            RecipeId = recipeId;
            BreakfastId = breakfastId;
            LunchId = lunchId;
        }

        public FakeWeeklyPlanRepository Plans { get; }

        public FakeWeeklyPlanReferenceRepository References { get; }

        public Guid RecipeId { get; }

        public Guid BreakfastId { get; }

        public Guid LunchId { get; }

        public static WeeklyPlanScenario Create()
        {
            var references = new FakeWeeklyPlanReferenceRepository();
            var dinner = MealType.Create("Cena", 2);
            var breakfast = MealType.Create("Desayuno", 0);
            var lunch = MealType.Create("Comida", 1);
            references.MealTypes.AddRange([dinner, breakfast, lunch]);
            var recipeId = Guid.NewGuid();
            references.RecipeIds.Add(recipeId);

            return new WeeklyPlanScenario(
                new FakeWeeklyPlanRepository(),
                references,
                recipeId,
                breakfast.Id,
                lunch.Id);
        }

        public WeeklyPlan AddPlan(string name, DateOnly startDate)
        {
            var plan = WeeklyPlan.Create(name, startDate, null);
            Plans.Items.Add(plan);
            return plan;
        }

        public WeeklyPlanService CreateService() => new(Plans, References);
    }

    private sealed class FakeWeeklyPlanRepository : IWeeklyPlanRepository
    {
        public List<WeeklyPlan> Items { get; } = [];

        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<WeeklyPlan>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<WeeklyPlan>>(Items);
        }

        public Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListSummariesAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<WeeklyPlanListItemResponse>>(
                Items.Select(item => new WeeklyPlanListItemResponse(
                    item.Id,
                    item.Name.Value,
                    item.StartDate,
                    item.EndDate)).ToArray());
        }

        public Task<WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<bool> ExistsByNormalizedNameAsync(
            string normalizedName,
            Guid? excludingId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.Any(item =>
                item.Name.Normalized == normalizedName && item.Id != excludingId));
        }

        public Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(plan);
            return Task.CompletedTask;
        }

        public void Remove(WeeklyPlan plan) => Items.Remove(plan);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWeeklyPlanReferenceRepository : IWeeklyPlanReferenceRepository
    {
        public List<MealType> MealTypes { get; } = [];

        public HashSet<Guid> RecipeIds { get; } = [];

        public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(RecipeIds.Contains(id));
        }

        public Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(MealTypes.Any(item => item.Id == id));
        }

        public Task<IReadOnlyList<MealType>> ListMealTypesAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<MealType>>(MealTypes);
        }
    }
}
