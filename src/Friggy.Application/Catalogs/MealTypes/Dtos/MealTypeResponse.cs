namespace Friggy.Application.Catalogs.MealTypes.Dtos;

/// <summary>
/// Datos de respuesta de un tipo de comida.
/// </summary>
public sealed record MealTypeResponse(Guid Id, string Name, int Order);
