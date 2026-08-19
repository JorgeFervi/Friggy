namespace Friggy.Application.Catalogs.MealTypes.Dtos;

/// <summary>
/// Datos necesarios para crear un tipo de comida.
/// </summary>
public sealed record CreateMealTypeRequest(string Name, int Order);
