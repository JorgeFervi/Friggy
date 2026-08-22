using System.Net;
using System.Text.Json;
using Friggy.Application.DailyPlanTemplates.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;

namespace Friggy.ComponentTests.DailyPlanTemplates;

public sealed class DailyPlanTemplatesApiClientTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public async Task TemplateOperations_UseDedicatedRoutes()
    {
        var response = new DailyPlanTemplateResponse(Guid.NewGuid(), "Laborable", []);
        Api.RespondWith("application/json", JsonSerializer.Serialize(new[] { response }));
        Api.RespondWith("application/json", JsonSerializer.Serialize(response));
        Api.RespondWith(HttpStatusCode.Created, "application/json", JsonSerializer.Serialize(response));
        Api.RespondWith("application/json", JsonSerializer.Serialize(response));
        Api.RespondWith(HttpStatusCode.Created, "application/json", JsonSerializer.Serialize(
            new ApplyDailyPlanTemplateResponse(response.Id, [])));
        Api.RespondWith(HttpStatusCode.NoContent);
        var client = new DailyPlanTemplatesApiClient(ApiClient);

        await client.ListAsync(TestContext.Current.CancellationToken);
        await client.GetAsync(response.Id, TestContext.Current.CancellationToken);
        await client.CreateAsync(new("Laborable", []), TestContext.Current.CancellationToken);
        await client.UpdateAsync(response.Id, new("Laborable", []), TestContext.Current.CancellationToken);
        await client.ApplyAsync(response.Id, new([new DateOnly(2030, 1, 8)]), TestContext.Current.CancellationToken);
        await client.DeleteAsync(response.Id, TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                (HttpMethod.Get, "api/daily-plan-templates"),
                (HttpMethod.Get, $"api/daily-plan-templates/{response.Id}"),
                (HttpMethod.Post, "api/daily-plan-templates"),
                (HttpMethod.Put, $"api/daily-plan-templates/{response.Id}"),
                (HttpMethod.Post, $"api/daily-plan-templates/{response.Id}/apply"),
                (HttpMethod.Delete, $"api/daily-plan-templates/{response.Id}"),
            ],
            Api.Requests.Select(item => (item.Method, item.Uri?.PathAndQuery.TrimStart('/'))));
    }
}
