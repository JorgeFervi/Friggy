using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.UnitTypes.Services;

public sealed class UnitTypeService(IUnitTypeRepository repository)
{
    public async Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken))
            .OrderBy(item => item.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(Map)
            .ToArray();

    public async Task<UnitTypeResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken));

    public async Task<UnitTypeResponse> CreateAsync(
        CreateUnitTypeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = UnitType.Create(
            request.Name,
            request.Symbol,
            ParseDimension(request.MeasurementDimension, MeasurementDimension.Unconverted),
            request.BaseUnitFactor ?? 1m,
            request.CanUseForCooking ?? true,
            request.CanUseForShopping ?? true);
        await EnsureUniqueAsync(item.Name.Normalized, null, cancellationToken);
        await EnsureShoppingPathAsync(item, null, cancellationToken);
        await repository.AddAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<UnitTypeResponse> UpdateAsync(
        Guid id,
        UpdateUnitTypeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await FindAsync(id, cancellationToken);
        var dimension = ParseDimension(request.MeasurementDimension, item.MeasurementDimension);
        var factor = request.BaseUnitFactor ?? item.BaseUnitFactor;
        var cooking = request.CanUseForCooking ?? item.CanUseForCooking;
        var shopping = request.CanUseForShopping ?? item.CanUseForShopping;
        if ((dimension != item.MeasurementDimension || factor != item.BaseUnitFactor) &&
            await repository.IsReferencedAsync(item.Id, cancellationToken))
        {
            throw new CatalogConflictException(
                "unit-type.conversion-metadata.in-use",
                "No se puede cambiar la conversión de una unidad que ya está en uso.");
        }

        var candidate = UnitType.Create(
            request.Name,
            request.Symbol,
            dimension,
            factor,
            cooking,
            shopping);
        await EnsureUniqueAsync(candidate.Name.Normalized, item.Id, cancellationToken);
        await EnsureShoppingPathAsync(candidate, item.Id, cancellationToken);
        item.Update(request.Name, request.Symbol, dimension, factor, cooking, shopping);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        EnsureShoppingPath((await repository.ListAsync(cancellationToken))
            .Where(candidate => candidate.Id != item.Id)
            .ToArray());
        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<UnitType> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ??
        throw new CatalogNotFoundException(
            "unit-type.not-found",
            "No se encontró la unidad.");

    private async Task EnsureUniqueAsync(
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNormalizedNameAsync(name, excludingId, cancellationToken))
        {
            throw new CatalogConflictException(
                "unit-type.name.duplicate",
                "Ya existe una unidad con ese nombre.");
        }
    }

    private async Task EnsureShoppingPathAsync(
        UnitType candidate,
        Guid? replacingId,
        CancellationToken cancellationToken)
    {
        var units = (await repository.ListAsync(cancellationToken))
            .Where(item => item.Id != replacingId)
            .Append(candidate)
            .ToArray();
        EnsureShoppingPath(units);
    }

    private static void EnsureShoppingPath(IReadOnlyCollection<UnitType> units)
    {
        if (units.Any(item =>
                item.MeasurementDimension is MeasurementDimension.Unconverted &&
                item.CanUseForCooking &&
                !item.CanUseForShopping))
        {
            throw new DomainValidationException(
                "unit-type.shopping-unit.required",
                "Una unidad sin conversión usada en cocina también debe poder usarse para compra.");
        }

        var invalid = units
            .Where(item => item.MeasurementDimension is not MeasurementDimension.Unconverted)
            .GroupBy(item => item.MeasurementDimension)
            .Any(group => group.Any(item => item.CanUseForCooking) &&
                !group.Any(item => item.CanUseForShopping));
        if (invalid)
        {
            throw new DomainValidationException(
                "unit-type.shopping-unit.required",
                "Cada dimensión usada en cocina necesita una unidad de compra.");
        }
    }

    private static MeasurementDimension ParseDimension(
        string? value,
        MeasurementDimension fallback) => value?.Trim().ToLowerInvariant() switch
        {
            null => fallback,
            "unconverted" => MeasurementDimension.Unconverted,
            "mass" => MeasurementDimension.Mass,
            "volume" => MeasurementDimension.Volume,
            "count" => MeasurementDimension.Count,
            _ => throw new DomainValidationException(
                "unit-type.measurement-dimension.invalid",
                "La dimensión de medida no es válida."),
        };

    private static UnitTypeResponse Map(UnitType item) => new(
        item.Id,
        item.Name.Value,
        item.Symbol)
    {
        MeasurementDimension = item.MeasurementDimension.ToString().ToLowerInvariant(),
        BaseUnitFactor = item.BaseUnitFactor,
        CanUseForCooking = item.CanUseForCooking,
        CanUseForShopping = item.CanUseForShopping,
    };
}
