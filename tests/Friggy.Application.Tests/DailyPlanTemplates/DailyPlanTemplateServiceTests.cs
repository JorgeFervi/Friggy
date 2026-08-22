using Friggy.Application.DailyPlanTemplates.Dtos;
using Friggy.Application.DailyPlanTemplates.Exceptions;
using Friggy.Application.DailyPlanTemplates.Interfaces;
using Friggy.Application.DailyPlanTemplates.Services;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlanTemplates;
using Friggy.Domain.DailyPlans;

namespace Friggy.Application.Tests.DailyPlanTemplates;

public sealed class DailyPlanTemplateServiceTests
{
    [Fact]
    public async Task Apply_SeparateDates_CreatesIndependentPlansAndSavesOnce()
    {
        var templates = new FakeTemplateRepository();
        var plans = new FakePlanRepository();
        var references = new FakeReferences();
        var template = DailyPlanTemplate.Create("Laborable");
        template.AddMeal(references.MealType.Id, references.RecipeId, 2, new TimeOnly(14, 30));
        templates.Items.Add(template);
        var service = new DailyPlanTemplateService(templates, plans, references);

        var response = await service.ApplyAsync(
            template.Id,
            new ApplyDailyPlanTemplateRequest([new DateOnly(2030, 1, 10), new DateOnly(2030, 1, 8)]),
            TestContext.Current.CancellationToken);

        Assert.Equal([new DateOnly(2030, 1, 8), new DateOnly(2030, 1, 10)], response.CreatedPlans.Select(plan => plan.Date));
        Assert.NotEqual(response.CreatedPlans[0].Id, response.CreatedPlans[1].Id);
        Assert.Equal(1, plans.SaveCount);
    }

    [Fact]
    public async Task Apply_AnyExistingDate_ThrowsConflictWithoutAddingPlans()
    {
        var templates = new FakeTemplateRepository();
        var plans = new FakePlanRepository();
        var references = new FakeReferences();
        var template = DailyPlanTemplate.Create("Laborable");
        template.AddMeal(references.MealType.Id, null, 1, null);
        templates.Items.Add(template);
        var occupied = new DateOnly(2030, 1, 8);
        plans.Items.Add(DailyPlan.Create(occupied));
        var service = new DailyPlanTemplateService(templates, plans, references);

        var exception = await Assert.ThrowsAsync<DailyPlanTemplateConflictException>(() =>
            service.ApplyAsync(
                template.Id,
                new ApplyDailyPlanTemplateRequest([occupied, new DateOnly(2030, 1, 10)]),
                TestContext.Current.CancellationToken));

        Assert.Equal("daily-plan-template.dates.conflict", exception.Code);
        Assert.Equal([occupied], exception.ConflictingDates);
        Assert.Single(plans.Items);
        Assert.Equal(0, plans.SaveCount);
    }

    private sealed class FakeTemplateRepository : IDailyPlanTemplateRepository
    {
        public List<DailyPlanTemplate> Items { get; } = [];

        public Task<IReadOnlyList<DailyPlanTemplate>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DailyPlanTemplate>>(Items);

        public Task<DailyPlanTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

        public Task AddAsync(DailyPlanTemplate planTemplate, CancellationToken cancellationToken)
        {
            Items.Add(planTemplate);
            return Task.CompletedTask;
        }

        public void Remove(DailyPlanTemplate planTemplate) => Items.Remove(planTemplate);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePlanRepository : IDailyPlanRepository
    {
        public List<DailyPlan> Items { get; } = [];

        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<DailyPlan>> ListBetweenAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DailyPlan>>([]);

        public Task<DailyPlan?> GetByDateAsync(DateOnly plannedDate, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Date == plannedDate));

        public Task<IReadOnlyList<DateOnly>> ListExistingDatesAsync(IReadOnlyCollection<DateOnly> dates, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DateOnly>>(Items.Where(item => dates.Contains(item.Date)).Select(item => item.Date).ToArray());

        public Task AddAsync(DailyPlan plan, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddRangeAsync(IReadOnlyCollection<DailyPlan> plans, CancellationToken cancellationToken)
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

    private sealed class FakeReferences : IDailyPlanReferenceRepository
    {
        public MealType MealType { get; } = MealType.Create("Comida", 0);

        public Guid RecipeId { get; } = Guid.NewGuid();

        public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == RecipeId);

        public Task<IReadOnlyDictionary<Guid, TimeSpan?>> GetRecipeEstimatedTimesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, TimeSpan?>>(new Dictionary<Guid, TimeSpan?>());

        public Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == MealType.Id);

        public Task<IReadOnlyList<MealType>> ListMealTypesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MealType>>([MealType]);
    }
}
