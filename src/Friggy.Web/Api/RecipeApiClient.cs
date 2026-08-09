using System.Net.Http.Json;
using System.Text.Json;
using Friggy.Application.Recipes.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Friggy.Web.Api;

public sealed class RecipeApiClient(HttpClient httpClient) : IRecipesApiClient
{
    public async Task<IReadOnlyList<RecipeListItemResponse>> ListAsync(
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/recipes", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<RecipeListItemResponse[]>(cancellationToken) ?? [];
    }

    public async Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/recipes/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRecipeAsync(response, cancellationToken);
    }

    public async Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken) =>
        await SendAsync(HttpMethod.Post, "api/recipes", request, cancellationToken);

    public async Task<RecipeResponse> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken) =>
        await SendAsync(HttpMethod.Put, $"api/recipes/{id}", request, cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync($"api/recipes/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<RecipeResponse> SendAsync(
        HttpMethod method,
        string uri,
        object body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(body),
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadRecipeAsync(response, cancellationToken);
    }

    private static async Task<RecipeResponse> ReadRecipeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<RecipeResponse>(cancellationToken) ??
        throw new RecipeApiException("La API devolvió una respuesta vacía.");

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            throw new RecipeApiException(
                problem?.Detail ?? problem?.Title ?? "No se pudo completar la operación.");
        }
        catch (JsonException)
        {
            throw new RecipeApiException("No se pudo completar la operación.");
        }
    }
}
