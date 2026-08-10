using System.Net;
using System.Text.Json;
using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;

namespace Friggy.ComponentTests.WeeklyPlans;

public sealed class WeeklyPlanApiClientTests : ComponentTest
{
    private static readonly Guid PlanId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid MealTypeId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid RecipeId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly DateOnly WeekStart = new(2026, 8, 3);

    [Fact]
    [Trait("Category", "Component")]
    public async Task Operations_UseExpectedRoutesAndDeserializeResponses()
    {
        var plan = CompletePlan();
        var json = JsonSerializer.Serialize(plan);
        Api.RespondWith("application/json", JsonSerializer.Serialize(
            new[] { new WeeklyPlanListItemResponse(PlanId, plan.Name, plan.StartDate, plan.EndDate) }));
        Api.RespondWith("application/json", json);
        Api.RespondWith(HttpStatusCode.Created, "application/json", json);
        Api.RespondWith("application/json", json);
        Api.RespondWith("application/json", json);
        Api.RespondWith("application/json", json);
        Api.RespondWith(HttpStatusCode.NoContent);
        var client = new WeeklyPlanApiClient(ApiClient);

        var listed = await client.ListAsync(TestContext.Current.CancellationToken);
        var loaded = await client.GetAsync(PlanId, TestContext.Current.CancellationToken);
        var created = await client.CreateAsync(
            new("Semana 32", WeekStart, null),
            TestContext.Current.CancellationToken);
        var updated = await client.UpdateAsync(
            PlanId,
            new("Semana actualizada", null),
            TestContext.Current.CancellationToken);
        var assigned = await client.SetEntryAsync(
            PlanId,
            WeekStart,
            MealTypeId,
            new(RecipeId),
            TestContext.Current.CancellationToken);
        var removed = await client.RemoveEntryAsync(
            PlanId,
            WeekStart,
            MealTypeId,
            TestContext.Current.CancellationToken);
        await client.DeleteAsync(PlanId, TestContext.Current.CancellationToken);

        Assert.Single(listed);
        Assert.Equal(PlanId, loaded.Id);
        Assert.Equal(PlanId, created.Id);
        Assert.Equal(PlanId, updated.Id);
        Assert.Equal(PlanId, assigned.Id);
        Assert.Equal(PlanId, removed.Id);
        Assert.Equal(
            [
                (HttpMethod.Get, "api/weekly-plans"),
                (HttpMethod.Get, $"api/weekly-plans/{PlanId}"),
                (HttpMethod.Post, "api/weekly-plans"),
                (HttpMethod.Put, $"api/weekly-plans/{PlanId}"),
                (HttpMethod.Put, $"api/weekly-plans/{PlanId}/days/2026-08-03/meal-types/{MealTypeId}"),
                (HttpMethod.Delete, $"api/weekly-plans/{PlanId}/days/2026-08-03/meal-types/{MealTypeId}"),
                (HttpMethod.Delete, $"api/weekly-plans/{PlanId}"),
            ],
            Api.Requests.Select(request => (request.Method, request.Uri?.PathAndQuery.TrimStart('/'))));
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task Get_ProblemDetails_ThrowsWeeklyPlanApiExceptionWithDetail()
    {
        Api.RespondWith(
            HttpStatusCode.NotFound,
            "application/problem+json",
            """{"title":"No encontrado","detail":"No se encontró el plan semanal."}""");
        var client = new WeeklyPlanApiClient(ApiClient);

        var exception = await Assert.ThrowsAsync<WeeklyPlanApiException>(
            () => client.GetAsync(PlanId, TestContext.Current.CancellationToken));

        Assert.Equal("No se encontró el plan semanal.", exception.Message);
    }

    private static WeeklyPlanResponse CompletePlan() =>
        new(
            PlanId,
            "Semana 32",
            WeekStart,
            WeekStart.AddDays(6),
            null,
            Enumerable.Range(0, 7)
                .Select(offset => new WeeklyPlanDayResponse(
                    WeekStart.AddDays(offset),
                    [new WeeklyPlanMealResponse(MealTypeId, "Comida", 1, null)]))
                .ToArray());
}
