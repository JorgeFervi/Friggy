using Friggy.Domain.Catalogs;

namespace Friggy.Domain.DailyPlans;

/// <summary>Representa un hueco de comida del plan diario.</summary>
public sealed class MealPlanSlot
{
    private MealPlanSlot()
    {
    }

    private MealPlanSlot(Guid id, Guid dailyPlanId, Guid mealTypeId, int order)
    {
        Id = id;
        DailyPlanId = dailyPlanId;
        MealTypeId = mealTypeId;
        Order = order;
    }

    /// <summary>Código técnico del hueco.</summary>
    public Guid Id { get; private set; }

    /// <summary>Código del plan diario propietario.</summary>
    public Guid DailyPlanId { get; private set; }

    /// <summary>Código del tipo de comida.</summary>
    public Guid MealTypeId { get; private set; }

    /// <summary>Orden del hueco dentro del día.</summary>
    public int Order { get; private set; }

    /// <summary>Hora prevista para la comida.</summary>
    public TimeOnly? PlannedTime { get; private set; }

    internal static MealPlanSlot Create(Guid dailyPlanId, Guid mealTypeId, int order) =>
        new(Guid.NewGuid(), dailyPlanId, mealTypeId, order);

    internal void MoveTo(int order) => Order = order;

    internal void SetPlannedTime(TimeOnly? plannedTime) => PlannedTime = plannedTime;

    /// <summary>Calcula el comienzo de preparación para la fecha del plan.</summary>
    public DateTime? GetPreparationStartsAt(DateOnly date, TimeSpan? estimatedTime)
    {
        if (estimatedTime < TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "daily-plan.slot.estimated-time.non-negative",
                "El tiempo estimado no puede ser negativo.");
        }

        return PlannedTime.HasValue && estimatedTime.HasValue
            ? date.ToDateTime(PlannedTime.Value, DateTimeKind.Unspecified)
                .Subtract(estimatedTime.Value)
            : null;
    }
}
