namespace Friggy.Application.Catalogs.UnitTypes.Dtos;

/// <summary>
/// Datos necesarios para crear una unidad de medida.
/// </summary>
public sealed record CreateUnitTypeRequest(string Name, string Symbol);
