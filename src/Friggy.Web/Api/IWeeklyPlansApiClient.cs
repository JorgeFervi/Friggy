using Friggy.Application.WeeklyPlans.Dtos;

namespace Friggy.Web.Api;

public interface IWeeklyPlansApiClient
{
    Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListAsync(CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> CreateAsync(CreateWeeklyPlanRequest request, CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> UpdateAsync(Guid id, UpdateWeeklyPlanRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> SetEntryAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> RemoveEntryAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        CancellationToken cancellationToken);
}
