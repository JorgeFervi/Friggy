using System.Net;
using System.Net.Http.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.IntegrationTests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.IntegrationTests.Api;

public sealed class CatalogEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task IngredientEndpoints_FullLifecycle_ReturnExpectedContracts()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest("Tomate"),
            TestContext.Current.CancellationToken);
        var ingredient = await response.Content.ReadFromJsonAsync<IngredientResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(ingredient);
        Assert.Equal("Tomate", ingredient.Name);
        Assert.Equal($"/api/ingredients/{ingredient.Id}", response.Headers.Location?.OriginalString);

        var loaded = await client.GetFromJsonAsync<IngredientResponse>(
            $"/api/ingredients/{ingredient.Id}",
            TestContext.Current.CancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/ingredients/{ingredient.Id}",
            new UpdateIngredientRequest("Cebolla"),
            TestContext.Current.CancellationToken);
        var updated = await updateResponse.Content.ReadFromJsonAsync<IngredientResponse>(
            TestContext.Current.CancellationToken);
        var listed = await client.GetFromJsonAsync<IngredientResponse[]>(
            "/api/ingredients",
            TestContext.Current.CancellationToken);
        using var deleteResponse = await client.DeleteAsync(
            $"/api/ingredients/{ingredient.Id}",
            TestContext.Current.CancellationToken);
        using var missingResponse = await client.GetAsync(
            $"/api/ingredients/{ingredient.Id}",
            TestContext.Current.CancellationToken);

        Assert.Equal("Tomate", loaded?.Name);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Cebolla", updated?.Name);
        Assert.Contains(listed ?? [], item => item.Id == ingredient.Id && item.Name == "Cebolla");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostIngredient_DuplicateNormalizedName_ReturnsConflictProblemDetails()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest("Tomate"),
            TestContext.Current.CancellationToken);

        using var response = await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest(" tomate "),
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("ingredient.name.duplicate", problem.Extensions["code"]?.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OtherCatalogRoutes_CreateUpdateAndDelete_ReturnExpectedContracts()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var unitResponse = await client.PostAsJsonAsync("/api/unit-types", new CreateUnitTypeRequest("Taza", "tza"), TestContext.Current.CancellationToken);
        using var tagResponse = await client.PostAsJsonAsync("/api/recipe-tags", new CreateRecipeTagRequest("Vegano"), TestContext.Current.CancellationToken);
        using var mealResponse = await client.PostAsJsonAsync("/api/meal-types", new CreateMealTypeRequest("Merienda", 3), TestContext.Current.CancellationToken);

        var unit = await unitResponse.Content.ReadFromJsonAsync<UnitTypeResponse>(TestContext.Current.CancellationToken);
        var tag = await tagResponse.Content.ReadFromJsonAsync<RecipeTagResponse>(TestContext.Current.CancellationToken);
        var meal = await mealResponse.Content.ReadFromJsonAsync<MealTypeResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, unitResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, tagResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, mealResponse.StatusCode);
        Assert.NotNull(unit);
        Assert.Equal("unconverted", unit.MeasurementDimension);
        Assert.Equal(1m, unit.BaseUnitFactor);
        Assert.True(unit.CanUseForCooking);
        Assert.True(unit.CanUseForShopping);
        Assert.NotNull(tag);
        Assert.NotNull(meal);

        using var updateUnit = await client.PutAsJsonAsync($"/api/unit-types/{unit.Id}", new UpdateUnitTypeRequest("Vaso", "vso"), TestContext.Current.CancellationToken);
        using var updateTag = await client.PutAsJsonAsync($"/api/recipe-tags/{tag.Id}", new UpdateRecipeTagRequest("Vegetariano"), TestContext.Current.CancellationToken);
        using var updateMeal = await client.PutAsJsonAsync($"/api/meal-types/{meal.Id}", new UpdateMealTypeRequest("Tentempié", 4), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updateUnit.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateTag.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateMeal.StatusCode);

        using var deleteUnit = await client.DeleteAsync($"/api/unit-types/{unit.Id}", TestContext.Current.CancellationToken);
        using var deleteTag = await client.DeleteAsync($"/api/recipe-tags/{tag.Id}", TestContext.Current.CancellationToken);
        using var deleteMeal = await client.DeleteAsync($"/api/meal-types/{meal.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteUnit.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteTag.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteMeal.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnitTypeEndpoint_FullMetadata_RoundTripsWireContract()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();
        var request = new CreateUnitTypeRequest("Cucharada", "cda")
        {
            MeasurementDimension = "volume",
            BaseUnitFactor = 15m,
            CanUseForCooking = true,
            CanUseForShopping = false,
        };

        using var response = await client.PostAsJsonAsync(
            "/api/unit-types",
            request,
            TestContext.Current.CancellationToken);
        var unit = await response.Content.ReadFromJsonAsync<UnitTypeResponse>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(unit);
        Assert.Equal("volume", unit.MeasurementDimension);
        Assert.Equal(15m, unit.BaseUnitFactor);
        Assert.True(unit.CanUseForCooking);
        Assert.False(unit.CanUseForShopping);
    }
}
