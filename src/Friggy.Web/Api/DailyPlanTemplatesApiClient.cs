using System.Net.Http.Json;
using Friggy.Application.DailyPlanTemplates.Dtos;

namespace Friggy.Web.Api;

public sealed class DailyPlanTemplatesApiClient(HttpClient httpClient) : IDailyPlanTemplatesApiClient
{
    public async Task<IReadOnlyList<DailyPlanTemplateResponse>> ListAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/daily-plan-templates", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<DailyPlanTemplateResponse[]>(cancellationToken) ?? [];
    }

    public Task<DailyPlanTemplateResponse> CreateAsync(CreateDailyPlanTemplateRequest request, CancellationToken cancellationToken) =>
        SendAsync<DailyPlanTemplateResponse>(HttpMethod.Post, "api/daily-plan-templates", request, cancellationToken);

    public Task<ApplyDailyPlanTemplateResponse> ApplyAsync(Guid id, ApplyDailyPlanTemplateRequest request, CancellationToken cancellationToken) =>
        SendAsync<ApplyDailyPlanTemplateResponse>(HttpMethod.Post, $"api/daily-plan-templates/{id}/apply", request, cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.DeleteAsync($"api/daily-plan-templates/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body) };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ??
            throw new ApiProblemException("La API devolvió una respuesta vacía.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiProblemException.FromResponseAsync(response, cancellationToken);
        }
    }
}
