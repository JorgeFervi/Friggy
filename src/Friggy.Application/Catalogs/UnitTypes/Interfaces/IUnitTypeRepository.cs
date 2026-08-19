using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.UnitTypes.Interfaces;

/// <summary>
/// Contrato de persistencia para las unidades de medida del catálogo.
/// </summary>
public interface IUnitTypeRepository
{
    /// <summary>
    /// Obtiene todas las unidades de medida.
    /// </summary>
    Task<IReadOnlyList<UnitType>> ListAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Busca una unidad de medida por su identificador.
    /// </summary>
    Task<UnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>
    /// Comprueba si ya existe una unidad con el nombre normalizado indicado.
    /// </summary>
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    /// <summary>
    /// Añade una unidad de medida para su persistencia.
    /// </summary>
    Task AddAsync(UnitType item, CancellationToken cancellationToken);
    /// <summary>
    /// Marca una unidad de medida para eliminarla.
    /// </summary>
    void Remove(UnitType item);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
