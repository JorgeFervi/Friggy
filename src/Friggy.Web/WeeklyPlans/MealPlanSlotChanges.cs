namespace Friggy.Web.WeeklyPlans;

public sealed record AddMealPlanSlotChange(DateOnly Date, Guid MealTypeId);

public sealed record ReorderMealPlanSlotsChange(DateOnly Date, IReadOnlyList<Guid> SlotIds);

public sealed record SetMealPlanSlotTimeChange(Guid SlotId, string? PlannedTime);

public sealed record SkipMealPlanEntryChange(
    DateOnly Date,
    Guid MealTypeId,
    string? Reason,
    string? AlternativeDescription);
