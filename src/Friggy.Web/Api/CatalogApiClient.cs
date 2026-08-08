using System.Net.Http.Json;
using System.Text.Json;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.Web.Api;

public sealed class CatalogApiClient(HttpClient httpClient) :
    IIngredientsApiClient,
    IUnitTypesApiClient,
    IRecipeTagsApiClient,
    IMealTypesApiClient
{
    async Task<IReadOnlyList<IngredientResponse>> IIngredientsApiClient.ListAsync(CancellationToken token) => await GetListAsync<IngredientResponse>("api/ingredients", token);
    async Task<IngredientResponse> IIngredientsApiClient.CreateAsync(CreateIngredientRequest request, CancellationToken token) => await SendAsync<IngredientResponse>(HttpMethod.Post, "api/ingredients", request, token);
    async Task<IngredientResponse> IIngredientsApiClient.UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken token) => await SendAsync<IngredientResponse>(HttpMethod.Put, $"api/ingredients/{id}", request, token);
    Task IIngredientsApiClient.DeleteAsync(Guid id, CancellationToken token) => DeleteAsync($"api/ingredients/{id}", token);

    async Task<IReadOnlyList<UnitTypeResponse>> IUnitTypesApiClient.ListAsync(CancellationToken token) => await GetListAsync<UnitTypeResponse>("api/unit-types", token);
    async Task<UnitTypeResponse> IUnitTypesApiClient.CreateAsync(CreateUnitTypeRequest request, CancellationToken token) => await SendAsync<UnitTypeResponse>(HttpMethod.Post, "api/unit-types", request, token);
    async Task<UnitTypeResponse> IUnitTypesApiClient.UpdateAsync(Guid id, UpdateUnitTypeRequest request, CancellationToken token) => await SendAsync<UnitTypeResponse>(HttpMethod.Put, $"api/unit-types/{id}", request, token);
    Task IUnitTypesApiClient.DeleteAsync(Guid id, CancellationToken token) => DeleteAsync($"api/unit-types/{id}", token);

    async Task<IReadOnlyList<RecipeTagResponse>> IRecipeTagsApiClient.ListAsync(CancellationToken token) => await GetListAsync<RecipeTagResponse>("api/recipe-tags", token);
    async Task<RecipeTagResponse> IRecipeTagsApiClient.CreateAsync(CreateRecipeTagRequest request, CancellationToken token) => await SendAsync<RecipeTagResponse>(HttpMethod.Post, "api/recipe-tags", request, token);
    async Task<RecipeTagResponse> IRecipeTagsApiClient.UpdateAsync(Guid id, UpdateRecipeTagRequest request, CancellationToken token) => await SendAsync<RecipeTagResponse>(HttpMethod.Put, $"api/recipe-tags/{id}", request, token);
    Task IRecipeTagsApiClient.DeleteAsync(Guid id, CancellationToken token) => DeleteAsync($"api/recipe-tags/{id}", token);

    async Task<IReadOnlyList<MealTypeResponse>> IMealTypesApiClient.ListAsync(CancellationToken token) => await GetListAsync<MealTypeResponse>("api/meal-types", token);
    async Task<MealTypeResponse> IMealTypesApiClient.CreateAsync(CreateMealTypeRequest request, CancellationToken token) => await SendAsync<MealTypeResponse>(HttpMethod.Post, "api/meal-types", request, token);
    async Task<MealTypeResponse> IMealTypesApiClient.UpdateAsync(Guid id, UpdateMealTypeRequest request, CancellationToken token) => await SendAsync<MealTypeResponse>(HttpMethod.Put, $"api/meal-types/{id}", request, token);
    Task IMealTypesApiClient.DeleteAsync(Guid id, CancellationToken token) => DeleteAsync($"api/meal-types/{id}", token);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T[]>(cancellationToken) ?? [];
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body) };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ??
            throw new CatalogApiException("La API devolvió una respuesta vacía.");
    }

    private async Task DeleteAsync(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            throw new CatalogApiException(problem?.Detail ?? problem?.Title ?? "No se pudo completar la operación.");
        }
        catch (JsonException)
        {
            throw new CatalogApiException("No se pudo completar la operación.");
        }
    }
}
