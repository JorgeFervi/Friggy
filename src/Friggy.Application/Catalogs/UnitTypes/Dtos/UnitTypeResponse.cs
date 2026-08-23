namespace Friggy.Application.Catalogs.UnitTypes.Dtos;

/// <summary>
/// Datos de respuesta de una unidad de medida.
/// </summary>
public sealed record UnitTypeResponse(Guid Id, string Name, string Symbol)
{
    public string MeasurementDimension { get; init; } = "unconverted";
    public decimal BaseUnitFactor { get; init; } = 1m;
    public bool CanUseForCooking { get; init; } = true;
    public bool CanUseForShopping { get; init; } = true;
}
