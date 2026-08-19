namespace Friggy.Application.Catalogs.UnitTypes.Dtos;

/// <summary>
/// Datos necesarios para actualizar una unidad de medida.
/// </summary>
public sealed record UpdateUnitTypeRequest(string Name, string Symbol);
