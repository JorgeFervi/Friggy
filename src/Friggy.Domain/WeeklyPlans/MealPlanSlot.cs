namespace Friggy.Domain.WeeklyPlans;

public sealed class MealPlanSlot
{
    private MealPlanSlot()
    {
    }

    private MealPlanSlot(
        Guid id,
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        int order)
    {
        Id = id;
        WeeklyPlanId = weeklyPlanId;
        Date = date;
        MealTypeId = mealTypeId;
        Order = order;
    }

    public Guid Id { get; private set; }

    public Guid WeeklyPlanId { get; private set; }

    public DateOnly Date { get; private set; }

    public Guid MealTypeId { get; private set; }

    public int Order { get; private set; }

    public TimeOnly? PlannedTime { get; private set; }

    internal static MealPlanSlot Create(
        Guid weeklyPlanId,
        DateOnly date,
        Guid mealTypeId,
        int order) =>
        new(Guid.NewGuid(), weeklyPlanId, date, mealTypeId, order);

    internal void MoveTo(int order) => Order = order;

    internal void SetPlannedTime(TimeOnly? plannedTime) => PlannedTime = plannedTime;

    public DateTime? GetPreparationStartsAt(TimeSpan? estimatedTime)
    {
        if (estimatedTime < TimeSpan.Zero)
        {
            throw new Catalogs.DomainValidationException(
                "weekly-plan.slot.estimated-time.non-negative",
                "El tiempo estimado no puede ser negativo.");
        }

        if (!PlannedTime.HasValue || !estimatedTime.HasValue)
        {
            return null;
        }

        var plannedAt = Date.ToDateTime(
            PlannedTime.Value,
            DateTimeKind.Unspecified);
        return plannedAt.Subtract(estimatedTime.Value);
    }
}
