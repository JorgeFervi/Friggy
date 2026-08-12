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
    Task<WeeklyPlanResponse> AddSlotAsync(
        Guid planId,
        DateOnly mealDate,
        AddMealPlanSlotRequest request,
        CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> ReorderSlotsAsync(
        Guid planId,
        DateOnly mealDate,
        ReorderMealPlanSlotsRequest request,
        CancellationToken cancellationToken);
    Task<WeeklyPlanResponse> RemoveSlotAsync(
        Guid planId,
        Guid slotId,
        CancellationToken cancellationToken);
    Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
        Guid planId,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        CancellationToken cancellationToken);
    Task<MealPlanEntryStateResponse> SkipEntryAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        CancellationToken cancellationToken);
}
