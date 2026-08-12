namespace Friggy.Application.WeeklyPlans.Dtos;

public sealed record ReorderMealPlanSlotsRequest(IReadOnlyList<Guid> SlotIds);
