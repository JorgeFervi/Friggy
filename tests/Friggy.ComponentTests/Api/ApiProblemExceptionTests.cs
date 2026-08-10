using System.Net;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;

namespace Friggy.ComponentTests.Api;

public sealed class ApiProblemExceptionTests : ComponentTest
{
    [Fact]
    public async Task Clients_CancelledOperation_DoesNotSendARequest()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IIngredientsApiClient catalog = new CatalogApiClient(ApiClient);
        var recipes = new RecipeApiClient(ApiClient);
        var weeklyPlans = new WeeklyPlanApiClient(ApiClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => catalog.ListAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => recipes.ListAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => weeklyPlans.ListAsync(cancellation.Token));

        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Clients_ProblemDetails_PreserveStatusAndPayload()
    {
        const string problem =
            """{"type":"https://friggy.test/problems/conflict","title":"Conflicto","status":409,"detail":"El nombre ya existe.","code":"catalog.name.duplicate"}""";
        Api.RespondWith(HttpStatusCode.Conflict, "application/problem+json", problem);
        Api.RespondWith(HttpStatusCode.Conflict, "application/problem+json", problem);
        Api.RespondWith(HttpStatusCode.Conflict, "application/problem+json", problem);
        IIngredientsApiClient catalog = new CatalogApiClient(ApiClient);
        var recipes = new RecipeApiClient(ApiClient);
        var weeklyPlans = new WeeklyPlanApiClient(ApiClient);

        var catalogException = await Assert.ThrowsAsync<ApiProblemException>(
            () => catalog.ListAsync(TestContext.Current.CancellationToken));
        var recipeException = await Assert.ThrowsAsync<ApiProblemException>(
            () => recipes.ListAsync(TestContext.Current.CancellationToken));
        var weeklyPlanException = await Assert.ThrowsAsync<ApiProblemException>(
            () => weeklyPlans.ListAsync(TestContext.Current.CancellationToken));

        Assert.All([catalogException, recipeException, weeklyPlanException], exception =>
        {
            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.Equal("El nombre ya existe.", exception.Message);
            Assert.Equal("Conflicto", exception.ProblemDetails.Title);
            Assert.True(exception.ProblemDetails.Extensions.TryGetValue("code", out var code));
            Assert.Equal(
                "catalog.name.duplicate",
                code?.ToString());
        });
    }

    [Fact]
    public async Task Client_NonProblemResponse_ProvidesActionableFallback()
    {
        Api.RespondWith(
            HttpStatusCode.ServiceUnavailable,
            "text/plain",
            "upstream unavailable");
        var client = new RecipeApiClient(ApiClient);

        var exception = await Assert.ThrowsAsync<ApiProblemException>(
            () => client.ListAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal("No se pudo completar la operación.", exception.Message);
        Assert.Equal(503, exception.ProblemDetails.Status);
    }
}
