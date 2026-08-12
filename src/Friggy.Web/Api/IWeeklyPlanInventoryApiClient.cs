using Friggy.Application.Inventory.Dtos;

namespace Friggy.Web.Api;

public interface IWeeklyPlanInventoryApiClient
{
    Task<IReadOnlyList<InventoryRequirementResponse>> GetRequirementsAsync(
        Guid planId,
        CancellationToken cancellationToken);

    Task<MealCompletionResponse> CompleteMealAsync(
        Guid planId,
        DateOnly mealDate,
        Guid mealTypeId,
        CompleteMealRequest request,
        CancellationToken cancellationToken);
}
