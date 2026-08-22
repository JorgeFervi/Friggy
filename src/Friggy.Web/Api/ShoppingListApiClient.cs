using System.Net.Http.Json;
using Friggy.Application.ShoppingLists.Dtos;

namespace Friggy.Web.Api;

public sealed class ShoppingListApiClient(HttpClient httpClient) : IShoppingListApiClient
{
    public async Task<ShoppingListResponse> GetAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/shopping-list?from={from:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiProblemException.FromResponseAsync(response, cancellationToken);
        }
        return await response.Content.ReadFromJsonAsync<ShoppingListResponse>(cancellationToken) ?? throw new ApiProblemException("La API devolvió una respuesta vacía.");
    }
}
