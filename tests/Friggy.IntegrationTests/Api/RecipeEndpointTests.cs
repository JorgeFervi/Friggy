using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Application.WeeklyPlans.Dtos;
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
        var firstLineId = Guid.NewGuid();
        var secondLineId = Guid.NewGuid();
        var createRequest = new CreateRecipeRequest(
            "Gazpacho",
            20,
            [
                new RecipeIngredientRequest(
                    ingredient.Id,
                    CatalogSeedIds.Unit,
                    2m,
                    1,
                    secondLineId),
                new RecipeIngredientRequest(
                    ingredient.Id,
                    CatalogSeedIds.Gram,
                    1.250m,
                    0,
                    firstLineId),
            ],
            [
                new RecipeStepRequest(
                    "Servir",
                    null,
                    1,
                    [secondLineId, firstLineId]),
                new RecipeStepRequest(
                    "Triturar",
                    5,
                    0,
                    [firstLineId]),
            ],
            [],
            [CatalogSeedIds.Lunch]);

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
        Assert.Equal([firstLineId, secondLineId], created.Ingredients.Select(item => item.Id));
        Assert.Equal([firstLineId], created.Steps[0].RecipeIngredientIds);
        Assert.Equal(
            [firstLineId, secondLineId],
            created.Steps[1].RecipeIngredientIds);

        var loaded = await client.GetFromJsonAsync<RecipeResponse>(
            $"/api/recipes/{created.Id}",
            TestContext.Current.CancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/recipes/{created.Id}",
            new UpdateRecipeRequest(
                "Salmorejo",
                25,
                [
                    new RecipeIngredientRequest(
                        ingredient.Id,
                        CatalogSeedIds.Gram,
                        1.5m,
                        1,
                        firstLineId),
                    new RecipeIngredientRequest(
                        ingredient.Id,
                        CatalogSeedIds.Unit,
                        3m,
                        0,
                        secondLineId),
                ],
                [
                    new RecipeStepRequest(
                        "Emulsionar",
                        5,
                        0,
                        [firstLineId, secondLineId]),
                ],
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
        Assert.Equal([firstLineId], loaded?.Steps[0].RecipeIngredientIds);
        Assert.Equal(
            [firstLineId, secondLineId],
            loaded?.Steps[1].RecipeIngredientIds);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Salmorejo", updated.Name);
        Assert.Equal([secondLineId, firstLineId], updated.Ingredients.Select(item => item.Id));
        Assert.Equal("Emulsionar", Assert.Single(updated.Steps).Description);
        Assert.Equal(
            [secondLineId, firstLineId],
            updated.Steps[0].RecipeIngredientIds);
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
    public async Task PostRecipe_UnknownIngredientLineInStep_ReturnsStableBadRequestWithoutPersistingRecipe()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client, "Tomate");
        var lineId = Guid.NewGuid();
        var request = new CreateRecipeRequest(
            "Inválida",
            20,
            [
                new RecipeIngredientRequest(
                    ingredient.Id,
                    CatalogSeedIds.Gram,
                    1m,
                    0,
                    lineId),
            ],
            [
                new RecipeStepRequest(
                    "Preparar",
                    null,
                    0,
                    [Guid.NewGuid()]),
            ],
            [],
            []);

        using var response = await client.PostAsJsonAsync(
            "/api/recipes",
            request,
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        var recipes = await client.GetFromJsonAsync<RecipeListItemResponse[]>(
            "/api/recipes",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("recipe-ingredient.not-found", problem.Extensions["code"]?.ToString());
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
    public async Task DeleteRecipe_AssignedToWeeklyPlan_ReturnsSafeConflictAndPreservesData()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var ingredient = await CreateIngredientAsync(client, "Tomate");
        using var createRecipeResponse = await client.PostAsJsonAsync(
            "/api/recipes",
            CreateRequest("Gazpacho", ingredient.Id),
            TestContext.Current.CancellationToken);
        var recipe = await createRecipeResponse.Content.ReadFromJsonAsync<RecipeResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(recipe);

        var weekStart = new DateOnly(2026, 8, 3);
        using var createPlanResponse = await client.PostAsJsonAsync(
            "/api/weekly-plans",
            new CreateWeeklyPlanRequest("Semana 32", weekStart, null),
            TestContext.Current.CancellationToken);
        var plan = await createPlanResponse.Content.ReadFromJsonAsync<WeeklyPlanResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(plan);
        using var assignResponse = await client.PutAsJsonAsync(
            $"/api/weekly-plans/{plan.Id}/days/{weekStart:yyyy-MM-dd}/meal-types/{CatalogSeedIds.Lunch}",
            new SetMealPlanEntryRequest(recipe.Id),
            TestContext.Current.CancellationToken);
        assignResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await client.DeleteAsync(
            $"/api/recipes/{recipe.Id}",
            TestContext.Current.CancellationToken);
        var problem = await deleteResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);
        using var recipeResponse = await client.GetAsync(
            $"/api/recipes/{recipe.Id}",
            TestContext.Current.CancellationToken);
        var preservedPlan = await client.GetFromJsonAsync<WeeklyPlanResponse>(
            $"/api/weekly-plans/{plan.Id}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("persistence.conflict", problem.Extensions["code"]?.ToString());
        Assert.Equal(
            "La operación entra en conflicto con el estado actual de los datos.",
            problem.Detail);
        Assert.Equal(HttpStatusCode.OK, recipeResponse.StatusCode);
        Assert.Contains(
            preservedPlan?.Days.SelectMany(day => day.Meals) ?? [],
            meal => meal.RecipeId == recipe.Id);
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
        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        var ingredientRequestSchema = FindSchema(schemas, "RecipeIngredientRequest");
        var stepRequestSchema = FindSchema(schemas, "RecipeStepRequest");
        var stepResponseSchema = FindSchema(schemas, "RecipeStepResponse");
        Assert.True(collection.TryGetProperty("get", out _));
        Assert.True(collection.TryGetProperty("post", out _));
        Assert.True(resource.TryGetProperty("get", out _));
        Assert.True(resource.TryGetProperty("put", out _));
        Assert.True(resource.TryGetProperty("delete", out _));
        Assert.True(
            resource
                .GetProperty("delete")
                .GetProperty("responses")
                .TryGetProperty("409", out _));
        Assert.Equal(
            "Crear una receta con asociaciones entre pasos e ingredientes",
            collection.GetProperty("post").GetProperty("summary").GetString());
        Assert.Equal(
            "Actualizar una receta y sus asociaciones entre pasos e ingredientes",
            resource.GetProperty("put").GetProperty("summary").GetString());
        Assert.True(
            ingredientRequestSchema.GetProperty("properties").TryGetProperty("id", out _));
        Assert.True(
            stepRequestSchema
                .GetProperty("properties")
                .TryGetProperty("recipeIngredientIds", out _));
        Assert.True(
            stepResponseSchema
                .GetProperty("properties")
                .TryGetProperty("recipeIngredientIds", out _));
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

    private static JsonElement FindSchema(JsonElement schemas, string suffix) =>
        schemas
            .EnumerateObject()
            .Single(item => item.Name.EndsWith(suffix, StringComparison.Ordinal))
            .Value;

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
