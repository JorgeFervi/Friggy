using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.UnitTypes.Services;

/// <summary>
/// Servicio de aplicación que gestiona el ciclo de vida de las unidades de medida.
/// </summary>
public sealed class UnitTypeService(IUnitTypeRepository repository)
{
    /// <summary>
    /// Obtiene las unidades ordenadas por nombre.
    /// </summary>
    public async Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).OrderBy(x => x.Name.Value, StringComparer.CurrentCultureIgnoreCase).Select(Map).ToArray();

    /// <summary>
    /// Obtiene una unidad de medida por su identificador.
    /// </summary>
    public async Task<UnitTypeResponse> GetAsync(Guid id, CancellationToken cancellationToken) => Map(await FindAsync(id, cancellationToken));

    /// <summary>
    /// Crea una unidad de medida y persiste sus cambios.
    /// </summary>
    public async Task<UnitTypeResponse> CreateAsync(CreateUnitTypeRequest request, CancellationToken cancellationToken)
    {
        var item = UnitType.Create(request.Name, request.Symbol);
        await EnsureUniqueAsync(item.Name.Normalized, null, cancellationToken);
        await repository.AddAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    /// <summary>
    /// Actualiza una unidad de medida y persiste sus cambios.
    /// </summary>
    public async Task<UnitTypeResponse> UpdateAsync(Guid id, UpdateUnitTypeRequest request, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        item.Update(request.Name, request.Symbol);
        await EnsureUniqueAsync(item.Name.Normalized, item.Id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    /// <summary>
    /// Elimina una unidad de medida y persiste sus cambios.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<UnitType> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new CatalogNotFoundException("unit-type.not-found", "No se encontró la unidad.");

    private async Task EnsureUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNormalizedNameAsync(name, excludingId, cancellationToken))
        {
            throw new CatalogConflictException("unit-type.name.duplicate", "Ya existe una unidad con ese nombre.");
        }
    }

    private static UnitTypeResponse Map(UnitType item) => new(item.Id, item.Name.Value, item.Symbol);
}
