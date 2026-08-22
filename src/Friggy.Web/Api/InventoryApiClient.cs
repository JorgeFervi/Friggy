using System.Net.Http.Json;
using Friggy.Application.Inventory.Dtos;

namespace Friggy.Web.Api;

public sealed class InventoryApiClient(HttpClient httpClient) : IInventoryApiClient
{
    public Task<IReadOnlyList<InventoryLotResponse>> ListAsync(
        bool includeUnavailable,
        CancellationToken cancellationToken) =>
        GetListAsync<InventoryLotResponse>(
            $"api/inventory-lots?includeUnavailable={includeUnavailable}",
            cancellationToken);

    public Task<InventoryLotResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync<InventoryLotResponse>($"api/inventory-lots/{id}", cancellationToken);

    public Task<InventoryLotResponse> CreateAsync(
        CreateInventoryLotRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<InventoryLotResponse>(
            HttpMethod.Post,
            "api/inventory-lots",
            request,
            cancellationToken);

    public Task<InventoryLotResponse> CorrectExpirationAsync(
        Guid id,
        CorrectInventoryExpirationRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<InventoryLotResponse>(
            HttpMethod.Put,
            $"api/inventory-lots/{id}/expiration",
            request,
            cancellationToken);

    public Task<InventoryOperationResponse> ConsumeAsync(
        Guid id,
        InventoryQuantityRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<InventoryOperationResponse>(
            HttpMethod.Post,
            $"api/inventory-lots/{id}/consume",
            request,
            cancellationToken);

    public Task<InventoryOperationResponse> DiscardAsync(
        Guid id,
        InventoryQuantityRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<InventoryOperationResponse>(
            HttpMethod.Post,
            $"api/inventory-lots/{id}/discard",
            request,
            cancellationToken);

    public Task<InventoryLotResponse> AdjustAsync(
        Guid id,
        AdjustInventoryLotRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<InventoryLotResponse>(
            HttpMethod.Post,
            $"api/inventory-lots/{id}/adjust",
            request,
            cancellationToken);

    private async Task<IReadOnlyList<T>> GetListAsync<T>(
        string uri,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T[]>(cancellationToken) ?? [];
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
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
        return await ReadAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<T>(cancellationToken) ??
        throw new ApiProblemException("La API devolvió una respuesta vacía.");

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiProblemException.FromResponseAsync(response, cancellationToken);
        }
    }
}
