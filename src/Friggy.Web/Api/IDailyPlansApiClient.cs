using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.Inventory.Dtos;

namespace Friggy.Web.Api;

public interface IDailyPlansApiClient
{
    Task<DailyPlanRangeResponse> ListAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);
    Task<DailyPlanResponse> GetAsync(DateOnly plannedDate, CancellationToken cancellationToken);
    Task<DailyPlanResponse> CreateAsync(
        CreateDailyPlanRequest request,
        CancellationToken cancellationToken);
    Task DeleteAsync(DateOnly plannedDate, CancellationToken cancellationToken);
    Task<DailyPlanResponse> SetEntryAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        CancellationToken cancellationToken);
    Task<DailyPlanResponse> RemoveEntryAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        CancellationToken cancellationToken);
    Task<DailyPlanResponse> AddSlotAsync(
        DateOnly plannedDate,
        AddMealPlanSlotRequest request,
        CancellationToken cancellationToken);
    Task<DailyPlanResponse> ReorderSlotsAsync(
        DateOnly plannedDate,
        ReorderMealPlanSlotsRequest request,
        CancellationToken cancellationToken);
    Task<DailyPlanResponse> RemoveSlotAsync(
        DateOnly plannedDate,
        Guid slotId,
        CancellationToken cancellationToken);
    Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
        DateOnly plannedDate,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        CancellationToken cancellationToken);
    Task<MealPlanEntryStateResponse> SkipEntryAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryRequirementResponse>> GetRequirementsAsync(
        DateOnly plannedDate,
        CancellationToken cancellationToken);
    Task<MealCompletionResponse> CompleteMealAsync(
        DateOnly plannedDate,
        Guid mealTypeId,
        CompleteMealRequest request,
        CancellationToken cancellationToken);
}
