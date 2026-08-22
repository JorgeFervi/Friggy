namespace Friggy.Web.DailyPlans;

public sealed record DailyPlanMealChange(Guid MealTypeId, Guid? RecipeId, int Servings);

public sealed record DailyPlanSlotTimeChange(Guid SlotId, string? PlannedTime);

public sealed record DailyPlanSlotAddChange(Guid MealTypeId);

public sealed record DailyPlanSlotsOrderChange(IReadOnlyList<Guid> SlotIds);

public sealed record DailyPlanSkipChange(
    Guid MealTypeId,
    string? Reason,
    string? AlternativeDescription);
