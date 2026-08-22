using System.Net;
using System.Net.Http.Json;
using Friggy.Application.DailyPlanTemplates.Dtos;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;

namespace Friggy.IntegrationTests.Api;

public sealed class DailyPlanTemplateEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAndApply_MultipleDates_MaterializesIndependentPlans()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var request = new CreateDailyPlanTemplateRequest(
            "Laborable",
            [new DailyPlanTemplateMealRequest(CatalogSeedIds.Lunch, null, 2, "14:30", 0)]);

        using var createResponse = await client.PostAsJsonAsync(
            "/api/daily-plan-templates",
            request,
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<DailyPlanTemplateResponse>(
            TestContext.Current.CancellationToken);
        using var applyResponse = await client.PostAsJsonAsync(
            $"/api/daily-plan-templates/{created?.Id}/apply",
            new ApplyDailyPlanTemplateRequest([new DateOnly(2030, 1, 8), new DateOnly(2030, 1, 10)]),
            TestContext.Current.CancellationToken);
        var applied = await applyResponse.Content.ReadFromJsonAsync<ApplyDailyPlanTemplateResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);
        Assert.Equal(2, applied?.CreatedPlans.Count);
        Assert.NotEqual(applied?.CreatedPlans[0].Id, applied?.CreatedPlans[1].Id);
        Assert.All(applied?.CreatedPlans ?? [], plan => Assert.Equal("14:30", Assert.Single(plan.Meals).PlannedTime));
    }
}
