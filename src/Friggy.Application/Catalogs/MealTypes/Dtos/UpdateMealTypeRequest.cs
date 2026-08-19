namespace Friggy.Application.Catalogs.MealTypes.Dtos;

/// <summary>
/// Datos necesarios para actualizar un tipo de comida.
/// </summary>
public sealed record UpdateMealTypeRequest(string Name, int Order);
