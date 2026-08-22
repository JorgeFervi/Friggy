using System.Globalization;
using System.Net.Http.Json;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.Inventory.Dtos;

namespace Friggy.Web.Api;

public sealed class DailyPlanApiClient(HttpClient httpClient) : IDailyPlansApiClient
{
    public async Task<DailyPlanRangeResponse> ListAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"api/daily-plans?from={Format(startDate)}&to={Format(endDate)}",
            cancellationToken);
        return await ReadAsync<DailyPlanRangeResponse>(response, cancellationToken);
    }

    public async Task<DailyPlanResponse> GetAsync(
        DateOnly plannedDate,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(BaseUri(plannedDate), cancellationToken);
        return await ReadAsync<DailyPlanResponse>(response, cancellationToken);
    }

    public Task<DailyPlanResponse> CreateAsync(
        CreateDailyPlanRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<DailyPlanResponse>(HttpMethod.Post, "api/daily-plans", request, cancellationToken);

    public async Task DeleteAsync(DateOnly plannedDate, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(BaseUri(plannedDate), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<DailyPlanResponse> SetEntryAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<DailyPlanResponse>(
            HttpMethod.Put,
            MealUri(plannedDate, mealTypeId),
            request,
            cancellationToken);

    public async Task<DailyPlanResponse> RemoveEntryAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(
            MealUri(plannedDate, mealTypeId),
            cancellationToken);
        return await ReadAsync<DailyPlanResponse>(response, cancellationToken);
    }

    public Task<DailyPlanResponse> AddSlotAsync(
        DateOnly plannedDate,
        AddMealPlanSlotRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<DailyPlanResponse>(
            HttpMethod.Post,
            $"{BaseUri(plannedDate)}/slots",
            request,
            cancellationToken);

    public Task<DailyPlanResponse> ReorderSlotsAsync(
        DateOnly plannedDate,
        ReorderMealPlanSlotsRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<DailyPlanResponse>(
            HttpMethod.Put,
            $"{BaseUri(plannedDate)}/slots/order",
            request,
            cancellationToken);

    public async Task<DailyPlanResponse> RemoveSlotAsync(
        DateOnly plannedDate,
        Guid slotId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync(
            $"{BaseUri(plannedDate)}/slots/{slotId}",
            cancellationToken);
        return await ReadAsync<DailyPlanResponse>(response, cancellationToken);
    }

    public Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
        DateOnly plannedDate,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<MealPlanSlotScheduleResponse>(
            HttpMethod.Put,
            $"{BaseUri(plannedDate)}/slots/{slotId}/time",
            request,
            cancellationToken);

    public Task<MealPlanEntryStateResponse> SkipEntryAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<MealPlanEntryStateResponse>(
            HttpMethod.Post,
            $"{MealUri(plannedDate, mealTypeId)}/skip",
            request,
            cancellationToken);

    public async Task<IReadOnlyList<InventoryRequirementResponse>> GetRequirementsAsync(
        DateOnly plannedDate,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"{BaseUri(plannedDate)}/inventory-requirements",
            cancellationToken);
        return await ReadAsync<InventoryRequirementResponse[]>(response, cancellationToken);
    }

    public Task<MealCompletionResponse> CompleteMealAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        CompleteMealRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<MealCompletionResponse>(
            HttpMethod.Post,
            $"{MealUri(plannedDate, mealTypeId)}/complete",
            request,
            cancellationToken);

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
        return await ReadAsync<T>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ??
            throw new ApiProblemException("La API devolvió una respuesta vacía.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiProblemException.FromResponseAsync(response, cancellationToken);
        }
    }

    private static string BaseUri(DateOnly date) => $"api/daily-plans/{Format(date)}";

    private static string MealUri(DateOnly date, Guid mealTypeId) =>
        $"{BaseUri(date)}/meal-types/{mealTypeId}";

    private static string Format(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
