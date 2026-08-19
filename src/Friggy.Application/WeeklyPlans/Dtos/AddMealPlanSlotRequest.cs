namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos necesarios para añadir un hueco de comida a un día del plan.
/// </summary>
public sealed record AddMealPlanSlotRequest(Guid MealTypeId);
