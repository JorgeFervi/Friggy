namespace Friggy.Application.Catalogs.UnitTypes.Dtos;

/// <summary>
/// Datos necesarios para actualizar una unidad de medida.
/// </summary>
public sealed record UpdateUnitTypeRequest(string Name, string Symbol)
{
    public string? MeasurementDimension { get; init; }
    public decimal? BaseUnitFactor { get; init; }
    public bool? CanUseForCooking { get; init; }
    public bool? CanUseForShopping { get; init; }
}
