using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Friggy.IntegrationTests.Api;

public sealed class WeeklyPlanEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private static readonly DateOnly WeekStart = new(2026, 8, 3);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WeeklyPlanEndpoints_FullLifecycle_ReturnExpectedContracts()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/weekly-plans",
            new CreateWeeklyPlanRequest("Semana 32", WeekStart, "Plan inicial"),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal($"/api/weekly-plans/{created.Id}", createResponse.Headers.Location?.OriginalString);
        Assert.Equal(WeekStart, created.StartDate);
        Assert.Equal(WeekStart.AddDays(6), created.EndDate);
        Assert.Equal(7, created.Days.Count);
        Assert.Equal(Enumerable.Range(0, 7).Select(WeekStart.AddDays), created.Days.Select(day => day.Date));
        Assert.All(created.Days, day =>
        {
            Assert.Equal(3, day.Meals.Count);
            Assert.Equal(day.Meals.OrderBy(meal => meal.MealTypeOrder), day.Meals);
            Assert.All(day.Meals, meal => Assert.Null(meal.RecipeId));
        });

        var loaded = await client.GetFromJsonAsync<WeeklyPlanResponse>(
            $"/api/weekly-plans/{created.Id}",
            TestContext.Current.CancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/weekly-plans/{created.Id}",
            new UpdateWeeklyPlanRequest("Semana actualizada", "Descripción actualizada"),
            TestContext.Current.CancellationToken);
        var updated = await updateResponse.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken);
        var listed = await client.GetFromJsonAsync<WeeklyPlanListItemResponse[]>(
            "/api/weekly-plans",
            TestContext.Current.CancellationToken);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/weekly-plans/{created.Id}",
            TestContext.Current.CancellationToken);
        using var missingResponse = await client.GetAsync(
            $"/api/weekly-plans/{created.Id}",
            TestContext.Current.CancellationToken);

        Assert.Equal("Semana 32", loaded?.Name);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Semana actualizada", updated.Name);
        Assert.Equal("Descripción actualizada", updated.Description);
        Assert.Contains(listed ?? [], plan => plan.Id == created.Id && plan.Name == "Semana actualizada");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CellEndpoints_RepeatedAssignmentReplacementAndRemoval_AreIdempotent()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var plan = await CreateWeeklyPlanAsync(client, "Semana idempotente");
        var ingredient = await CreateIngredientAsync(client, "Tomate");
        var firstRecipe = await CreateRecipeAsync(client, "Gazpacho", ingredient.Id);
        var replacement = await CreateRecipeAsync(client, "Salmorejo", ingredient.Id);
        var date = WeekStart.AddDays(1);
        var path = CellPath(plan.Id, date, CatalogSeedIds.Lunch);

        using var firstResponse = await client.PutAsJsonAsync(
            path,
            new SetMealPlanEntryRequest(firstRecipe.Id),
            TestContext.Current.CancellationToken);
        using var repeatedResponse = await client.PutAsJsonAsync(
            path,
            new SetMealPlanEntryRequest(firstRecipe.Id),
            TestContext.Current.CancellationToken);
        using var replacementResponse = await client.PutAsJsonAsync(
            path,
            new SetMealPlanEntryRequest(replacement.Id),
            TestContext.Current.CancellationToken);
        var replaced = await replacementResponse.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replacementResponse.StatusCode);
        Assert.NotNull(replaced);
        Assert.Equal(
            replacement.Id,
            GetMeal(replaced, date, CatalogSeedIds.Lunch).RecipeId);

        await using (var context = Database.CreateDbContext())
        {
            var entries = await context.MealPlanEntries.ToArrayAsync(
                TestContext.Current.CancellationToken);
            var entry = Assert.Single(entries);
            Assert.Equal(replacement.Id, entry.RecipeId);
        }

        var loaded = await client.GetFromJsonAsync<WeeklyPlanResponse>(
            $"/api/weekly-plans/{plan.Id}",
            TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(replacement.Id, GetMeal(loaded, date, CatalogSeedIds.Lunch).RecipeId);

        using var removeResponse = await client.DeleteAsync(
            path,
            TestContext.Current.CancellationToken);
        var removed = await removeResponse.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken);
        using var repeatedRemoveResponse = await client.DeleteAsync(
            path,
            TestContext.Current.CancellationToken);
        var repeatedlyRemoved = await repeatedRemoveResponse.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        Assert.NotNull(removed);
        Assert.Null(GetMeal(removed, date, CatalogSeedIds.Lunch).RecipeId);
        Assert.Equal(HttpStatusCode.OK, repeatedRemoveResponse.StatusCode);
        Assert.NotNull(repeatedlyRemoved);
        Assert.Null(GetMeal(repeatedlyRemoved, date, CatalogSeedIds.Lunch).RecipeId);

        await using var verificationContext = Database.CreateDbContext();
        Assert.Equal(
            0,
            await verificationContext.MealPlanEntries.CountAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PutCell_DateOutsideWeek_ReturnsBadRequestProblemDetails()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var plan = await CreateWeeklyPlanAsync(client, "Semana acotada");
        var ingredient = await CreateIngredientAsync(client, "Calabacín");
        var recipe = await CreateRecipeAsync(client, "Crema", ingredient.Id);

        using var response = await client.PutAsJsonAsync(
            CellPath(plan.Id, WeekStart.AddDays(7), CatalogSeedIds.Dinner),
            new SetMealPlanEntryRequest(recipe.Id),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("weekly-plan.entry.date.out-of-range", problem.Extensions["code"]?.ToString());
    }

    [Theory]
    [InlineData("2026-8-04")]
    [InlineData("04-08-2026")]
    [InlineData("not-a-date")]
    [Trait("Category", "Integration")]
    public async Task PutCell_DateIsNotIsoFormat_ReturnsBadRequestProblemDetails(string date)
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var plan = await CreateWeeklyPlanAsync(client, $"Semana {date}");

        using var response = await client.PutAsJsonAsync(
            $"/api/weekly-plans/{plan.Id}/days/{date}/meal-types/{CatalogSeedIds.Dinner}",
            new SetMealPlanEntryRequest(Guid.NewGuid()),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("weekly-plan.date.format", problem.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PutCell_MissingReferences_ReturnNotFoundProblemDetails()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var plan = await CreateWeeklyPlanAsync(client, "Semana referencias");
        var ingredient = await CreateIngredientAsync(client, "Pimiento");
        var recipe = await CreateRecipeAsync(client, "Pimientos asados", ingredient.Id);

        using var missingRecipeResponse = await client.PutAsJsonAsync(
            CellPath(plan.Id, WeekStart, CatalogSeedIds.Lunch),
            new SetMealPlanEntryRequest(Guid.NewGuid()),
            TestContext.Current.CancellationToken);
        var missingRecipeProblem = await missingRecipeResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        using var missingMealTypeResponse = await client.PutAsJsonAsync(
            CellPath(plan.Id, WeekStart, Guid.NewGuid()),
            new SetMealPlanEntryRequest(recipe.Id),
            TestContext.Current.CancellationToken);
        var missingMealTypeProblem = await missingMealTypeResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, missingRecipeResponse.StatusCode);
        Assert.Equal("weekly-plan.recipe.not-found", missingRecipeProblem?.Extensions["code"]?.ToString());
        Assert.Equal(HttpStatusCode.NotFound, missingMealTypeResponse.StatusCode);
        Assert.Equal("weekly-plan.meal-type.not-found", missingMealTypeProblem?.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostWeeklyPlan_InvalidOrDuplicateName_ReturnsProblemDetails()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        await CreateWeeklyPlanAsync(client, "Semana única");

        using var duplicateResponse = await client.PostAsJsonAsync(
            "/api/weekly-plans",
            new CreateWeeklyPlanRequest(" semana única ", WeekStart.AddDays(7), null),
            TestContext.Current.CancellationToken);
        var duplicateProblem = await duplicateResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        using var invalidStartResponse = await client.PostAsJsonAsync(
            "/api/weekly-plans",
            new CreateWeeklyPlanRequest("Semana inválida", WeekStart.AddDays(1), null),
            TestContext.Current.CancellationToken);
        var invalidStartProblem = await invalidStartResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal("weekly-plan.name.duplicate", duplicateProblem?.Extensions["code"]?.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, invalidStartResponse.StatusCode);
        Assert.Equal("weekly-plan.start-date.monday", invalidStartProblem?.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OpenApi_WeeklyPlanPaths_DescribeCrudAndCellOperations()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/openapi/v1.json",
            TestContext.Current.CancellationToken);
        await using var content = await response.Content.ReadAsStreamAsync(
            TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(
            content,
            cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var paths = document.RootElement.GetProperty("paths");
        var collection = paths.GetProperty("/api/weekly-plans");
        var resource = paths.GetProperty("/api/weekly-plans/{id}");
        var cell = paths.GetProperty(
            "/api/weekly-plans/{planId}/days/{date}/meal-types/{mealTypeId}");
        Assert.True(collection.TryGetProperty("get", out _));
        Assert.True(collection.TryGetProperty("post", out _));
        Assert.True(resource.TryGetProperty("get", out _));
        Assert.True(resource.TryGetProperty("put", out _));
        Assert.True(resource.TryGetProperty("delete", out _));
        Assert.Contains("yyyy-MM-dd", cell.GetProperty("put").GetProperty("description").GetString());
        Assert.Contains("yyyy-MM-dd", cell.GetProperty("delete").GetProperty("description").GetString());
    }

    private static async Task<WeeklyPlanResponse> CreateWeeklyPlanAsync(
        HttpClient client,
        string name)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/weekly-plans",
            new CreateWeeklyPlanRequest(name, WeekStart, null),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
                TestContext.Current.CancellationToken) ??
            throw new InvalidOperationException("La API no devolvió el plan semanal creado.");
    }

    private static async Task<IngredientResponse> CreateIngredientAsync(
        HttpClient client,
        string name)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest(name),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientResponse>(
                TestContext.Current.CancellationToken) ??
            throw new InvalidOperationException("La API no devolvió el ingrediente creado.");
    }

    private static async Task<RecipeResponse> CreateRecipeAsync(
        HttpClient client,
        string name,
        Guid ingredientId)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                name,
                20,
                [new RecipeIngredientRequest(ingredientId, CatalogSeedIds.Gram, 1m, 0)],
                [new RecipeStepRequest("Preparar", null, 0)],
                [],
                [CatalogSeedIds.Lunch]),
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeResponse>(
                TestContext.Current.CancellationToken) ??
            throw new InvalidOperationException("La API no devolvió la receta creada.");
    }

    private static WeeklyPlanMealResponse GetMeal(
        WeeklyPlanResponse plan,
        DateOnly date,
        Guid mealTypeId) =>
        Assert.Single(
            Assert.Single(plan.Days, day => day.Date == date).Meals,
            meal => meal.MealTypeId == mealTypeId);

    private static string CellPath(Guid planId, DateOnly date, Guid mealTypeId) =>
        $"/api/weekly-plans/{planId}/days/{date:yyyy-MM-dd}/meal-types/{mealTypeId}";
}
