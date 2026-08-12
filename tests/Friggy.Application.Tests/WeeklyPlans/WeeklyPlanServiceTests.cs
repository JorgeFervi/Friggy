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
        var stored = Assert.Single(scenario.Plans.Items);
        Assert.Equal(21, stored.Slots.Count);
        Assert.All(
            stored.Dates,
            date => Assert.Equal(
                [0, 1, 2],
                stored.Slots
                    .Where(slot => slot.Date == date)
                    .OrderBy(slot => slot.Order)
                    .Select(slot => slot.Order)));
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
            new SetMealPlanEntryRequest(scenario.RecipeId, 3),
            TestContext.Current.CancellationToken);

        Assert.Equal(scenario.RecipeId, Assert.Single(plan.Entries).RecipeId);
        Assert.Equal(3, Assert.Single(plan.Entries).Servings);
        Assert.Equal(1, scenario.Plans.SaveCount);
        var assignedDay = result.Days.Single(day => day.Date == plan.StartDate.AddDays(1));
        Assert.Equal(
            scenario.RecipeId,
            assignedDay.Meals.Single(meal => meal.MealTypeId == scenario.BreakfastId).RecipeId);
        Assert.Equal(
            3,
            assignedDay.Meals.Single(meal => meal.MealTypeId == scenario.BreakfastId).Servings);
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

    [Fact]
    public async Task SetSlotTime_ValidExactFormat_PersistsAndReturnsDerivedLocalStart()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var slot = plan.AddSlot(plan.StartDate, scenario.LunchId);
        plan.Assign(plan.StartDate, scenario.LunchId, scenario.RecipeId);
        var service = scenario.CreateService();

        var result = await service.SetSlotTimeAsync(
            plan.Id,
            slot.Id,
            new SetMealPlanSlotTimeRequest("14:05"),
            TestContext.Current.CancellationToken);

        Assert.Equal(slot.Id, result.SlotId);
        Assert.Equal("14:05", result.PlannedTime);
        Assert.Equal(
            new DateTime(2026, 8, 3, 13, 20, 0, DateTimeKind.Unspecified),
            result.PreparationStartsAt);
        Assert.Equal(DateTimeKind.Unspecified, result.PreparationStartsAt?.Kind);
        Assert.Equal(new TimeOnly(14, 5), slot.PlannedTime);
        Assert.Equal(1, scenario.Plans.SaveCount);
    }

    [Theory]
    [InlineData("14:5")]
    [InlineData("24:00")]
    [InlineData("14:05:00")]
    [InlineData("comida")]
    public async Task SetSlotTime_InvalidFormat_ThrowsAndDoesNotMutate(string plannedTime)
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var slot = plan.AddSlot(plan.StartDate, scenario.LunchId);
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.SetSlotTimeAsync(
                plan.Id,
                slot.Id,
                new SetMealPlanSlotTimeRequest(plannedTime),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.slot.planned-time.invalid", exception.Code);
        Assert.Null(slot.PlannedTime);
        Assert.Equal(0, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SetSlotTime_BlankValue_ClearsTimeWithoutPreparationStart()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var slot = plan.AddSlot(plan.StartDate, scenario.LunchId);
        plan.SetSlotTime(slot.Id, new TimeOnly(14, 5));
        var service = scenario.CreateService();

        var result = await service.SetSlotTimeAsync(
            plan.Id,
            slot.Id,
            new SetMealPlanSlotTimeRequest(" "),
            TestContext.Current.CancellationToken);

        Assert.Null(result.PlannedTime);
        Assert.Null(result.PreparationStartsAt);
        Assert.Null(slot.PlannedTime);
        Assert.Equal(1, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SetSlotTime_UnassignedSlot_ReturnsNoPreparationStart()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        var slot = plan.AddSlot(plan.StartDate, scenario.LunchId);
        var service = scenario.CreateService();

        var result = await service.SetSlotTimeAsync(
            plan.Id,
            slot.Id,
            new SetMealPlanSlotTimeRequest("00:30"),
            TestContext.Current.CancellationToken);

        Assert.Equal("00:30", result.PlannedTime);
        Assert.Null(result.PreparationStartsAt);
    }

    [Fact]
    public async Task SkipEntry_PlannedMeal_PersistsAndReturnsState()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        plan.Assign(plan.StartDate, scenario.LunchId, scenario.RecipeId);
        var service = scenario.CreateService();

        var result = await service.SkipEntryAsync(
            plan.Id,
            plan.StartDate,
            scenario.LunchId,
            new SkipMealPlanEntryRequest("  Viaje  ", "  Bocadillo  "),
            TestContext.Current.CancellationToken);

        Assert.Equal(Assert.Single(plan.Entries).Id, result.EntryId);
        Assert.Equal(MealPlanEntryStatus.Skipped, result.Status);
        Assert.Null(result.CompletedAt);
        Assert.Equal("Viaje", result.SkippedReason);
        Assert.Equal("Bocadillo", result.AlternativeDescription);
        Assert.Equal(1, scenario.Plans.SaveCount);
    }

    [Fact]
    public async Task SkipEntry_BlankReason_DoesNotSave()
    {
        var scenario = WeeklyPlanScenario.Create();
        var plan = scenario.AddPlan("Semana 32", new DateOnly(2026, 8, 3));
        plan.Assign(plan.StartDate, scenario.LunchId, scenario.RecipeId);
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.SkipEntryAsync(
                plan.Id,
                plan.StartDate,
                scenario.LunchId,
                new SkipMealPlanEntryRequest(" ", null),
                TestContext.Current.CancellationToken));

        Assert.Equal("weekly-plan.entry.skipped-reason.required", exception.Code);
        Assert.Equal(0, scenario.Plans.SaveCount);
        Assert.False(Assert.Single(plan.Entries).IsSkipped);
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
            references.RecipeEstimatedTimes.Add(recipeId, TimeSpan.FromMinutes(45));

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

        public Dictionary<Guid, TimeSpan> RecipeEstimatedTimes { get; } = [];

        public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(RecipeEstimatedTimes.ContainsKey(id));
        }

        public Task<TimeSpan?> GetRecipeEstimatedTimeAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                RecipeEstimatedTimes.TryGetValue(id, out var estimatedTime)
                    ? (TimeSpan?)estimatedTime
                    : null);
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
