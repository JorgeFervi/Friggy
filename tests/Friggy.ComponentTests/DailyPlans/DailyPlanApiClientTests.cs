using System.Net;
using System.Text.Json;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;

namespace Friggy.ComponentTests.DailyPlans;

public sealed class DailyPlanApiClientTests : ComponentTest
{
    private static readonly DateOnly PlannedDate = new(2026, 8, 4);
    private static readonly Guid PlanId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid MealTypeId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid RecipeId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid SlotId = Guid.Parse("70000000-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Category", "Component")]
    public async Task PlanningOperations_UseDateBasedRoutesAndDeserializeResponses()
    {
        var plan = CompletePlan();
        var planJson = JsonSerializer.Serialize(plan);
        Api.RespondWith("application/json", JsonSerializer.Serialize(
            new DailyPlanRangeResponse(PlannedDate, PlannedDate, [plan])));
        Api.RespondWith("application/json", planJson);
        Api.RespondWith(HttpStatusCode.Created, "application/json", planJson);
        Api.RespondWith("application/json", planJson);
        Api.RespondWith("application/json", planJson);
        Api.RespondWith("application/json", planJson);
        Api.RespondWith("application/json", planJson);
        Api.RespondWith("application/json", planJson);
        Api.RespondWith("application/json", JsonSerializer.Serialize(
            new MealPlanSlotScheduleResponse(SlotId, "14:00", null)));
        Api.RespondWith("application/json", JsonSerializer.Serialize(
            new MealPlanEntryStateResponse(
                Guid.NewGuid(),
                MealPlanEntryState.Skipped,
                null,
                "Viaje",
                null)));
        Api.RespondWith(HttpStatusCode.NoContent);
        var client = new DailyPlanApiClient(ApiClient);

        var range = await client.ListAsync(
            PlannedDate,
            PlannedDate,
            TestContext.Current.CancellationToken);
        await client.GetAsync(PlannedDate, TestContext.Current.CancellationToken);
        await client.CreateAsync(new(PlannedDate), TestContext.Current.CancellationToken);
        await client.SetEntryAsync(
            PlannedDate,
            MealTypeId,
            new(RecipeId, 2),
            TestContext.Current.CancellationToken);
        await client.RemoveEntryAsync(
            PlannedDate,
            MealTypeId,
            TestContext.Current.CancellationToken);
        await client.AddSlotAsync(
            PlannedDate,
            new(MealTypeId),
            TestContext.Current.CancellationToken);
        await client.ReorderSlotsAsync(
            PlannedDate,
            new([SlotId]),
            TestContext.Current.CancellationToken);
        await client.RemoveSlotAsync(
            PlannedDate,
            SlotId,
            TestContext.Current.CancellationToken);
        await client.SetSlotTimeAsync(
            PlannedDate,
            SlotId,
            new("14:00"),
            TestContext.Current.CancellationToken);
        await client.SkipEntryAsync(
            PlannedDate,
            MealTypeId,
            new("Viaje", null),
            TestContext.Current.CancellationToken);
        await client.DeleteAsync(PlannedDate, TestContext.Current.CancellationToken);

        Assert.Single(range.Plans);
        Assert.Equal(
            [
                (HttpMethod.Get, "api/daily-plans?from=2026-08-04&to=2026-08-04"),
                (HttpMethod.Get, "api/daily-plans/2026-08-04"),
                (HttpMethod.Post, "api/daily-plans"),
                (HttpMethod.Put, $"api/daily-plans/2026-08-04/meal-types/{MealTypeId}"),
                (HttpMethod.Delete, $"api/daily-plans/2026-08-04/meal-types/{MealTypeId}"),
                (HttpMethod.Post, "api/daily-plans/2026-08-04/slots"),
                (HttpMethod.Put, "api/daily-plans/2026-08-04/slots/order"),
                (HttpMethod.Delete, $"api/daily-plans/2026-08-04/slots/{SlotId}"),
                (HttpMethod.Put, $"api/daily-plans/2026-08-04/slots/{SlotId}/time"),
                (HttpMethod.Post, $"api/daily-plans/2026-08-04/meal-types/{MealTypeId}/skip"),
                (HttpMethod.Delete, "api/daily-plans/2026-08-04"),
            ],
            Api.Requests.Select(request =>
                (request.Method, request.Uri?.PathAndQuery.TrimStart('/'))));
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task Get_ProblemDetails_ThrowsDetail()
    {
        Api.RespondWith(
            HttpStatusCode.NotFound,
            "application/problem+json",
            """{"detail":"No se encontró el plan diario."}""");
        var client = new DailyPlanApiClient(ApiClient);

        var exception = await Assert.ThrowsAsync<ApiProblemException>(() =>
            client.GetAsync(PlannedDate, TestContext.Current.CancellationToken));

        Assert.Equal("No se encontró el plan diario.", exception.Message);
    }

    private static DailyPlanResponse CompletePlan() =>
        new(
            PlanId,
            PlannedDate,
            [new DailyPlanMealResponse(
                MealTypeId,
                "Comida",
                1,
                null,
                1,
                false,
                null,
                SlotId,
                0,
                null,
                null,
                MealPlanEntryState.Planned,
                null,
                null)]);
}
