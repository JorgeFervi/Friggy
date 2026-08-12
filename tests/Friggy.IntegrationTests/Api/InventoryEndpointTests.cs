using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;

namespace Friggy.IntegrationTests.Api;

public sealed class InventoryEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task InventoryAndMealCompletion_FullHttpJourney_ReturnsStableContracts()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client);
        var recipe = await CreateRecipeAsync(client, ingredient.Id);
        var plan = await CreatePlanAsync(client);
        var cellPath = $"/api/weekly-plans/{plan.Id}/days/{plan.StartDate:yyyy-MM-dd}/" +
            $"meal-types/{CatalogSeedIds.Lunch}";
        using var assignmentResponse = await client.PutAsJsonAsync(
            cellPath,
            new SetMealPlanEntryRequest(recipe.Id, 2),
            TestContext.Current.CancellationToken);
        assignmentResponse.EnsureSuccessStatusCode();

        using var createLotResponse = await client.PostAsJsonAsync(
            "/api/inventory-lots",
            new CreateInventoryLotRequest(
                ingredient.Id,
                CatalogSeedIds.Gram,
                3m,
                DateOnly.FromDateTime(DateTime.Today).AddDays(5)),
            TestContext.Current.CancellationToken);
        var lot = await createLotResponse.Content.ReadFromJsonAsync<InventoryLotResponse>(
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createLotResponse.StatusCode);
        Assert.NotNull(lot);
        Assert.Single(lot.Movements);

        var requirements = await client.GetFromJsonAsync<InventoryRequirementResponse[]>(
            $"/api/weekly-plans/{plan.Id}/inventory-requirements",
            TestContext.Current.CancellationToken);
        var requirement = Assert.Single(requirements ?? []);
        Assert.Equal(2m, requirement.RequiredQuantity);
        Assert.Equal(3m, requirement.AvailableQuantity);

        using var completeResponse = await client.PostAsJsonAsync(
            $"{cellPath}/complete",
            new CompleteMealRequest([new(lot.Id, 2m)]),
            TestContext.Current.CancellationToken);
        var completed = await completeResponse.Content.ReadFromJsonAsync<MealCompletionResponse>(
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.NotNull(completed);
        Assert.False(completed.AlreadyCompleted);
        Assert.Empty(completed.RemainingRequirements ?? []);

        using var retryResponse = await client.PostAsJsonAsync(
            $"{cellPath}/complete",
            new CompleteMealRequest([new(lot.Id, 2m)]),
            TestContext.Current.CancellationToken);
        var retry = await retryResponse.Content.ReadFromJsonAsync<MealCompletionResponse>(
            TestContext.Current.CancellationToken);
        var storedLot = await client.GetFromJsonAsync<InventoryLotResponse>(
            $"/api/inventory-lots/{lot.Id}",
            TestContext.Current.CancellationToken);
        Assert.True(retry?.AlreadyCompleted);
        Assert.Equal(1m, storedLot?.Quantity);
        Assert.Equal(2, storedLot?.Movements.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConsumeInventoryLot_NonPositiveQuantity_ReturnsStableValidationProblem()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client);
        using var createResponse = await client.PostAsJsonAsync(
            "/api/inventory-lots",
            new CreateInventoryLotRequest(
                ingredient.Id,
                CatalogSeedIds.Gram,
                1m,
                DateOnly.FromDateTime(DateTime.Today).AddDays(1)),
            TestContext.Current.CancellationToken);
        var lot = await createResponse.Content.ReadFromJsonAsync<InventoryLotResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(lot);

        using var response = await client.PostAsJsonAsync(
            $"/api/inventory-lots/{lot.Id}/consume",
            new InventoryQuantityRequest(0m),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "inventory-lot.operation.quantity.positive",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SkippedAndCompletedMeals_OnlyCompletedMealConsumesInventory()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client);
        var recipe = await CreateRecipeAsync(client, ingredient.Id);
        var plan = await CreatePlanAsync(client);
        var skippedDate = plan.StartDate;
        var completedDate = plan.StartDate.AddDays(1);
        var skippedPath = CellPath(plan.Id, skippedDate);
        var completedPath = CellPath(plan.Id, completedDate);

        using var skippedAssignment = await client.PutAsJsonAsync(
            skippedPath,
            new SetMealPlanEntryRequest(recipe.Id),
            TestContext.Current.CancellationToken);
        using var completedAssignment = await client.PutAsJsonAsync(
            completedPath,
            new SetMealPlanEntryRequest(recipe.Id),
            TestContext.Current.CancellationToken);
        using var createLotResponse = await client.PostAsJsonAsync(
            "/api/inventory-lots",
            new CreateInventoryLotRequest(
                ingredient.Id,
                CatalogSeedIds.Gram,
                3m,
                DateOnly.FromDateTime(DateTime.Today).AddDays(5)),
            TestContext.Current.CancellationToken);
        var lot = await createLotResponse.Content.ReadFromJsonAsync<InventoryLotResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(lot);

        using var skipResponse = await client.PostAsJsonAsync(
            $"{skippedPath}/skip",
            new SkipMealPlanEntryRequest("Viaje", "Bocadillo"),
            TestContext.Current.CancellationToken);
        var requirementsAfterSkip = await client.GetFromJsonAsync<InventoryRequirementResponse[]>(
            $"/api/weekly-plans/{plan.Id}/inventory-requirements",
            TestContext.Current.CancellationToken);
        using var completeResponse = await client.PostAsJsonAsync(
            $"{completedPath}/complete",
            new CompleteMealRequest([new(lot.Id, 1m)]),
            TestContext.Current.CancellationToken);
        var storedLot = await client.GetFromJsonAsync<InventoryLotResponse>(
            $"/api/inventory-lots/{lot.Id}",
            TestContext.Current.CancellationToken);
        var loadedPlan = await client.GetFromJsonAsync<WeeklyPlanResponse>(
            $"/api/weekly-plans/{plan.Id}",
            TestContext.Current.CancellationToken);

        skippedAssignment.EnsureSuccessStatusCode();
        completedAssignment.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, skipResponse.StatusCode);
        Assert.Equal(1m, Assert.Single(requirementsAfterSkip ?? []).RequiredQuantity);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.Equal(2m, storedLot?.Quantity);
        Assert.Equal(2, storedLot?.Movements.Count);
        Assert.NotNull(loadedPlan);
        Assert.Equal(
            MealPlanEntryState.Skipped,
            GetMeal(loadedPlan, skippedDate).Status);
        Assert.Equal(
            MealPlanEntryState.Completed,
            GetMeal(loadedPlan, completedDate).Status);
    }

    private static string CellPath(Guid planId, DateOnly date) =>
        $"/api/weekly-plans/{planId}/days/{date:yyyy-MM-dd}/meal-types/{CatalogSeedIds.Lunch}";

    private static WeeklyPlanMealResponse GetMeal(WeeklyPlanResponse plan, DateOnly date) =>
        plan.Days.Single(day => day.Date == date).Meals
            .Single(meal => meal.MealTypeId == CatalogSeedIds.Lunch);

    private static async Task<IngredientResponse> CreateIngredientAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest("Tomate"),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IngredientResponse>(
            TestContext.Current.CancellationToken))!;
    }

    private static async Task<RecipeResponse> CreateRecipeAsync(
        HttpClient client,
        Guid ingredientId)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                "Tomate preparado",
                10,
                [new RecipeIngredientRequest(ingredientId, CatalogSeedIds.Gram, 1m, 0)],
                [new RecipeStepRequest("Preparar", null, 0)],
                [],
                [CatalogSeedIds.Lunch]),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecipeResponse>(
            TestContext.Current.CancellationToken))!;
    }

    private static async Task<WeeklyPlanResponse> CreatePlanAsync(HttpClient client)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        while (startDate.DayOfWeek is not DayOfWeek.Monday)
        {
            startDate = startDate.AddDays(-1);
        }

        using var response = await client.PostAsJsonAsync(
            "/api/weekly-plans",
            new CreateWeeklyPlanRequest("Semana inventario", startDate, null),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken))!;
    }
}
