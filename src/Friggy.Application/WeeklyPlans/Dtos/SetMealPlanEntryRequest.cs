namespace Friggy.Application.WeeklyPlans.Dtos;

/// <summary>
/// Datos necesarios para asignar o reemplazar una receta en una comida.
/// </summary>
public sealed record SetMealPlanEntryRequest(Guid RecipeId, int Servings = 1);
