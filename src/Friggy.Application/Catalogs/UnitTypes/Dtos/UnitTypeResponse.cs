namespace Friggy.Application.Catalogs.UnitTypes.Dtos;

/// <summary>
/// Datos de respuesta de una unidad de medida.
/// </summary>
public sealed record UnitTypeResponse(Guid Id, string Name, string Symbol);
