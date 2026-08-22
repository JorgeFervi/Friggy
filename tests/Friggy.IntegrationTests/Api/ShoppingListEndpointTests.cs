using System.Net;
using System.Net.Http.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Application.ShoppingLists.Dtos;
using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.IntegrationTests.Api;

public sealed class ShoppingListEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Get_CombinedPlannedMealsAndInventory_ReturnsCalculatedComparison()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client);
        var recipe = await CreateRecipeAsync(client, ingredient.Id);
        await CreateAndAssignAsync(client, new(2030, 1, 6), recipe.Id);
        await CreateAndAssignAsync(client, new(2030, 1, 8), recipe.Id);
        using var lotResponse = await client.PostAsJsonAsync("/api/inventory-lots",
            new CreateInventoryLotRequest(ingredient.Id, CatalogSeedIds.Gram, 50m, new(2030, 12, 31)), TestContext.Current.CancellationToken);
        lotResponse.EnsureSuccessStatusCode();

        var result = await client.GetFromJsonAsync<ShoppingListResponse>("/api/shopping-list?from=2030-01-06&to=2030-01-08", TestContext.Current.CancellationToken);

        var item = Assert.Single(result?.Items ?? []);
        Assert.Equal((400m, 50m, 350m), (item.RequiredQuantity, item.AvailableQuantity, item.QuantityToBuy));
    }

    [Theory]
    [InlineData("/api/shopping-list?from=2030-01-06")]
    [InlineData("/api/shopping-list?from=06-01-2030&to=2030-01-08")]
    [InlineData("/api/shopping-list?from=2030-01-09&to=2030-01-08")]
    [Trait("Category", "Integration")]
    public async Task Get_InvalidQuery_ReturnsStableBadRequest(string route)
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(route, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.StartsWith("shopping-list.date", problem?.Extensions["code"]?.ToString());
    }

    private static async Task<IngredientResponse> CreateIngredientAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/ingredients", new CreateIngredientRequest("Tomate"), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientResponse>(TestContext.Current.CancellationToken) ?? throw new InvalidOperationException();
    }

    private static async Task<RecipeResponse> CreateRecipeAsync(HttpClient client, Guid ingredientId)
    {
        using var response = await client.PostAsJsonAsync("/api/recipes", new CreateRecipeRequest("Sopa", 20,
            [new RecipeIngredientRequest(ingredientId, CatalogSeedIds.Gram, 100m, 0)], [new RecipeStepRequest("Preparar", null, 0)], [], [CatalogSeedIds.Lunch]), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeResponse>(TestContext.Current.CancellationToken) ?? throw new InvalidOperationException();
    }

    private static async Task CreateAndAssignAsync(HttpClient client, DateOnly date, Guid recipeId)
    {
        using var create = await client.PostAsJsonAsync("/api/daily-plans", new CreateDailyPlanRequest(date), TestContext.Current.CancellationToken);
        create.EnsureSuccessStatusCode();
        using var assign = await client.PutAsJsonAsync($"/api/daily-plans/{date:yyyy-MM-dd}/meal-types/{CatalogSeedIds.Lunch}", new SetMealPlanEntryRequest(recipeId, 2), TestContext.Current.CancellationToken);
        assign.EnsureSuccessStatusCode();
    }
}
