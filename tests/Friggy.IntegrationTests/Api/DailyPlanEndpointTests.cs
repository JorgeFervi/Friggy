using System.Net;
using System.Net.Http.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.IntegrationTests.Api;

public sealed class DailyPlanEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private static readonly DateOnly PlannedDate = new(2026, 8, 4);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DailyPlanEndpoints_FullLifecycle_UsesDateAsResourceAddress()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        using var createResponse = await client.PostAsJsonAsync(
            "/api/daily-plans",
            new CreateDailyPlanRequest(PlannedDate),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<DailyPlanResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("/api/daily-plans/2026-08-04", createResponse.Headers.Location?.OriginalString);
        Assert.Equal(3, created.Meals.Count);

        var loaded = await client.GetFromJsonAsync<DailyPlanResponse>(
            "/api/daily-plans/2026-08-04",
            TestContext.Current.CancellationToken);
        var range = await client.GetFromJsonAsync<DailyPlanRangeResponse>(
            "/api/daily-plans?from=2026-08-04&to=2026-08-04",
            TestContext.Current.CancellationToken);
        using var deleteResponse = await client.DeleteAsync(
            "/api/daily-plans/2026-08-04",
            TestContext.Current.CancellationToken);

        Assert.Equal(created.Id, loaded?.Id);
        Assert.Single(range?.Plans ?? []);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Create_SameDateTwice_ReturnsDailyDateConflict()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        await CreatePlanAsync(client);

        using var response = await client.PostAsJsonAsync(
            "/api/daily-plans",
            new CreateDailyPlanRequest(PlannedDate),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("daily-plan.date.duplicate", problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Range_FromAfterTo_ReturnsValidationProblem()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/daily-plans?from=2026-08-05&to=2026-08-04",
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("daily-plan.date-range.invalid", problem?.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MealEndpoint_AssignsRecipeAndServingsToSingleDay()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        await CreatePlanAsync(client);
        var ingredient = await CreateIngredientAsync(client);
        var recipe = await CreateRecipeAsync(client, ingredient.Id);

        using var response = await client.PutAsJsonAsync(
            $"/api/daily-plans/2026-08-04/meal-types/{CatalogSeedIds.Lunch}",
            new SetMealPlanEntryRequest(recipe.Id, 4),
            TestContext.Current.CancellationToken);
        var plan = await response.Content.ReadFromJsonAsync<DailyPlanResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lunch = Assert.Single(plan?.Meals ?? [], meal => meal.MealTypeId == CatalogSeedIds.Lunch);
        Assert.Equal(recipe.Id, lunch.RecipeId);
        Assert.Equal(4, lunch.Servings);
    }

    private static async Task<DailyPlanResponse> CreatePlanAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/daily-plans",
            new CreateDailyPlanRequest(PlannedDate),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DailyPlanResponse>(
                TestContext.Current.CancellationToken) ??
            throw new InvalidOperationException("La API no devolvió el plan diario creado.");
    }

    private static async Task<IngredientResponse> CreateIngredientAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest("Tomate"),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientResponse>(
                TestContext.Current.CancellationToken) ??
            throw new InvalidOperationException("La API no devolvió el ingrediente.");
    }

    private static async Task<RecipeResponse> CreateRecipeAsync(HttpClient client, Guid ingredientId)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                "Gazpacho",
                20,
                [new RecipeIngredientRequest(ingredientId, CatalogSeedIds.Gram, 1m, 0)],
                [new RecipeStepRequest("Preparar", null, 0)],
                [],
                [CatalogSeedIds.Lunch]),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeResponse>(
                TestContext.Current.CancellationToken) ??
            throw new InvalidOperationException("La API no devolvió la receta.");
    }
}
