namespace Friggy.Application.Catalogs.UnitTypes.Dtos;

/// <summary>
/// Datos necesarios para crear una unidad de medida.
/// </summary>
public sealed record CreateUnitTypeRequest(string Name, string Symbol)
{
    public string? MeasurementDimension { get; init; }
    public decimal? BaseUnitFactor { get; init; }
    public bool? CanUseForCooking { get; init; }
    public bool? CanUseForShopping { get; init; }
}
