namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Orden solicitado para los huecos de comida de un día.
/// </summary>
public sealed record ReorderMealPlanSlotsRequest(IReadOnlyList<Guid> SlotIds);
