using Friggy.Domain.Catalogs;

namespace Friggy.Application.Inventory.Interfaces;

/// <summary>
/// Contrato para consultar los catálogos necesarios para operar con inventario.
/// </summary>
public interface IInventoryReferenceRepository
{
    /// <summary>
    /// Obtiene los ingredientes disponibles como referencia.
    /// </summary>
    Task<IReadOnlyList<Ingredient>> ListIngredientsAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene las unidades de medida disponibles como referencia.
    /// </summary>
    Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
        CancellationToken cancellationToken);
}
