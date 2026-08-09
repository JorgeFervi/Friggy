using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Domain.Catalogs;
using Friggy.IntegrationTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.IntegrationTests.Api;

public sealed class RecipeEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RecipeEndpoints_FullLifecycle_ReturnExpectedContracts()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client, "Tomate");
        var createRequest = CreateRequest(
            "Gazpacho",
            ingredient.Id,
            quantity: 1.250m,
            stepDescription: "Triturar");

        using var createResponse = await client.PostAsJsonAsync(
            "/api/recipes",
            createRequest,
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Gazpacho", created.Name);
        Assert.Equal($"/api/recipes/{created.Id}", createResponse.Headers.Location?.OriginalString);
        Assert.Equal(1.250m, Assert.Single(created.Ingredients).Quantity);
        Assert.Equal("Triturar", Assert.Single(created.Steps).Description);

        var loaded = await client.GetFromJsonAsync<RecipeResponse>(
            $"/api/recipes/{created.Id}",
            TestContext.Current.CancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/recipes/{created.Id}",
            new UpdateRecipeRequest(
                "Salmorejo",
                25,
                [new RecipeIngredientRequest(ingredient.Id, CatalogSeedIds.Gram, 2m, 0)],
                [new RecipeStepRequest("Emulsionar", 5, 0)],
                [],
                [CatalogSeedIds.Dinner]),
            TestContext.Current.CancellationToken);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RecipeResponse>(
            TestContext.Current.CancellationToken);
        var listed = await client.GetFromJsonAsync<RecipeListItemResponse[]>(
            "/api/recipes",
            TestContext.Current.CancellationToken);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/recipes/{created.Id}",
            TestContext.Current.CancellationToken);
        using var missingResponse = await client.GetAsync(
            $"/api/recipes/{created.Id}",
            TestContext.Current.CancellationToken);

        Assert.Equal("Gazpacho", loaded?.Name);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Salmorejo", updated.Name);
        Assert.Equal(2m, Assert.Single(updated.Ingredients).Quantity);
        Assert.Equal("Emulsionar", Assert.Single(updated.Steps).Description);
        Assert.Equal(CatalogSeedIds.Dinner, Assert.Single(updated.MealTypeIds));
        Assert.Contains(listed ?? [], item => item.Id == created.Id && item.Name == "Salmorejo");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostRecipe_NonPositiveQuantity_ReturnsBadRequestProblemDetails()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRequest("Inválida", Guid.NewGuid(), quantity: 0m),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("recipe-ingredient.quantity.positive", problem.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostRecipe_MissingIngredient_ReturnsNotFoundWithoutPersistingRecipe()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRequest("Sin referencia", Guid.NewGuid()),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        var recipes = await client.GetFromJsonAsync<RecipeListItemResponse[]>(
            "/api/recipes",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("recipe.ingredient.not-found", problem.Extensions["code"]?.ToString());
        Assert.Empty(recipes ?? []);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostRecipe_DuplicateNormalizedName_ReturnsConflictProblemDetails()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client, "Tomate");
        await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRequest("Gazpacho", ingredient.Id),
            TestContext.Current.CancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRequest(" gazpacho ", ingredient.Id),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("recipe.name.duplicate", problem.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OpenApi_RecipesPath_DescribesAllCrudOperations()
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
        var collection = paths.GetProperty("/api/recipes");
        var resource = paths.GetProperty("/api/recipes/{id}");
        Assert.True(collection.TryGetProperty("get", out _));
        Assert.True(collection.TryGetProperty("post", out _));
        Assert.True(resource.TryGetProperty("get", out _));
        Assert.True(resource.TryGetProperty("put", out _));
        Assert.True(resource.TryGetProperty("delete", out _));
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

    private static CreateRecipeRequest CreateRequest(
        string name,
        Guid ingredientId,
        decimal quantity = 1m,
        string stepDescription = "Preparar") =>
        new(
            name,
            20,
            [new RecipeIngredientRequest(ingredientId, CatalogSeedIds.Gram, quantity, 0)],
            [new RecipeStepRequest(stepDescription, null, 0)],
            [],
            [CatalogSeedIds.Lunch]);
}
