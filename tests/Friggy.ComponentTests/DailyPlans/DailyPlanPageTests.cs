using Bunit;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.Inventory.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.DailyPlans;

public sealed class DailyPlanPageTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void DailyPlans_EmptyInterval_DistinguishesEveryUnplannedDate()
    {
        Services.AddSingleton<IDailyPlansApiClient>(new StubDailyPlansApiClient());

        var component = Render<global::Friggy.Web.Components.Pages.DailyPlans>();

        component.WaitForAssertion(() =>
        {
            Assert.Equal("Planes diarios", component.Find("h1").TextContent);
            Assert.Equal(7, component.FindAll(".friggy-daily-plan-card").Count);
            Assert.Equal(7, component.Markup.Split("Sin plan").Length - 1);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void DailyPlans_ExistingDate_ShowsOpenActionAndNoCreateActionForThatDate()
    {
        var api = new StubDailyPlansApiClient();
        api.Plans.Add(new DailyPlanResponse(
            Guid.NewGuid(),
            api.Today,
            []));
        Services.AddSingleton<IDailyPlansApiClient>(api);

        var component = Render<global::Friggy.Web.Components.Pages.DailyPlans>();

        component.WaitForAssertion(() =>
        {
            Assert.Single(component.FindAll("a[href^='daily-plans/']"));
            Assert.Equal(6, component.FindAll("button").Count(button =>
                button.TextContent.Contains("Planificar este día", StringComparison.Ordinal)));
        });
    }

    private sealed class StubDailyPlansApiClient : IDailyPlansApiClient
    {
        public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);
        public List<DailyPlanResponse> Plans { get; } = [];

        public Task<DailyPlanRangeResponse> ListAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DailyPlanRangeResponse(startDate, endDate, Plans));

        public Task<DailyPlanResponse> GetAsync(DateOnly plannedDate, CancellationToken cancellationToken) =>
            Task.FromResult(Plans.Single(plan => plan.Date == plannedDate));

        public Task<DailyPlanResponse> CreateAsync(CreateDailyPlanRequest request, CancellationToken cancellationToken)
        {
            var plan = new DailyPlanResponse(Guid.NewGuid(), request.Date, []);
            Plans.Add(plan);
            return Task.FromResult(plan);
        }

        public Task DeleteAsync(DateOnly plannedDate, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<DailyPlanResponse> SetEntryAsync(DateOnly plannedDate, Guid mealTypeId, SetMealPlanEntryRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DailyPlanResponse> RemoveEntryAsync(DateOnly plannedDate, Guid mealTypeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DailyPlanResponse> AddSlotAsync(DateOnly plannedDate, AddMealPlanSlotRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DailyPlanResponse> ReorderSlotsAsync(DateOnly plannedDate, ReorderMealPlanSlotsRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DailyPlanResponse> RemoveSlotAsync(DateOnly plannedDate, Guid slotId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(DateOnly plannedDate, Guid slotId, SetMealPlanSlotTimeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MealPlanEntryStateResponse> SkipEntryAsync(DateOnly plannedDate, Guid mealTypeId, SkipMealPlanEntryRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<InventoryRequirementResponse>> GetRequirementsAsync(DateOnly plannedDate, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<InventoryRequirementResponse>>([]);
        public Task<MealCompletionResponse> CompleteMealAsync(DateOnly plannedDate, Guid mealTypeId, CompleteMealRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
