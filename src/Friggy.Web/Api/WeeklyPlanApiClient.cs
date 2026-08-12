using System.Globalization;
using System.Net.Http.Json;
using Friggy.Application.WeeklyPlans.Dtos;

namespace Friggy.Web.Api;

public sealed class WeeklyPlanApiClient(HttpClient httpClient) : IWeeklyPlansApiClient
{
    public async Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListAsync(
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/weekly-plans", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WeeklyPlanListItemResponse[]>(cancellationToken) ?? [];
    }

    public async Task<WeeklyPlanResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"api/weekly-plans/{id}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadPlanAsync(response, cancellationToken);
    }

    public async Task<WeeklyPlanResponse> CreateAsync(
        CreateWeeklyPlanRequest request,
        CancellationToken cancellationToken) =>
        await SendAsync(
            HttpMethod.Post,
            "api/weekly-plans",
            request,
            cancellationToken);

    public async Task<WeeklyPlanResponse> UpdateAsync(
        Guid id,
        UpdateWeeklyPlanRequest request,
        CancellationToken cancellationToken) =>
        await SendAsync(
            HttpMethod.Put,
            $"api/weekly-plans/{id}",
            request,
            cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/weekly-plans/{id}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<WeeklyPlanResponse> SetEntryAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        CancellationToken cancellationToken) =>
        await SendAsync(
            HttpMethod.Put,
            CellUri(planId, mealDate, mealTypeId),
            request,
            cancellationToken);

    public async Task<WeeklyPlanResponse> RemoveEntryAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(
            CellUri(planId, mealDate, mealTypeId),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadPlanAsync(response, cancellationToken);
    }

    public Task<WeeklyPlanResponse> AddSlotAsync(
        Guid planId,
        DateOnly mealDate,
        AddMealPlanSlotRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Post,
            DaySlotsUri(planId, mealDate),
            request,
            cancellationToken);

    public Task<WeeklyPlanResponse> ReorderSlotsAsync(
        Guid planId,
        DateOnly mealDate,
        ReorderMealPlanSlotsRequest request,
        CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Put,
            $"{DaySlotsUri(planId, mealDate)}/order",
            request,
            cancellationToken);

    public async Task<WeeklyPlanResponse> RemoveSlotAsync(
        Guid planId,
        Guid slotId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/weekly-plans/{planId}/slots/{slotId}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadPlanAsync(response, cancellationToken);
    }

    public async Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
        Guid planId,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        CancellationToken cancellationToken) =>
        await SendAndReadAsync<MealPlanSlotScheduleResponse>(
            HttpMethod.Put,
            $"api/weekly-plans/{planId}/slots/{slotId}/time",
            request,
            cancellationToken);

    public async Task<MealPlanEntryStateResponse> SkipEntryAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        CancellationToken cancellationToken) =>
        await SendAndReadAsync<MealPlanEntryStateResponse>(
            HttpMethod.Post,
            $"{CellUri(planId, mealDate, mealTypeId)}/skip",
            request,
            cancellationToken);

    private async Task<WeeklyPlanResponse> SendAsync(
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
        return await ReadPlanAsync(response, cancellationToken);
    }

    private async Task<T> SendAndReadAsync<T>(
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
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ??
            throw new ApiProblemException("La API devolvió una respuesta vacía.");
    }

    private static string CellUri(Guid planId, DateOnly date, Guid mealTypeId) =>
        $"api/weekly-plans/{planId}/days/" +
        $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/meal-types/{mealTypeId}";

    private static string DaySlotsUri(Guid planId, DateOnly date) =>
        $"api/weekly-plans/{planId}/days/" +
        $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/slots";

    private static async Task<WeeklyPlanResponse> ReadPlanAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken) ??
        throw new ApiProblemException("La API devolvió una respuesta vacía.");

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw await ApiProblemException.FromResponseAsync(response, cancellationToken);
    }
}
