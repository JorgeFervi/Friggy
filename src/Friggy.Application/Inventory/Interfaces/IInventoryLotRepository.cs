using Friggy.Domain.Inventory;

namespace Friggy.Application.Inventory.Interfaces;

/// <summary>
/// Contrato de persistencia para los lotes de inventario.
/// </summary>
public interface IInventoryLotRepository
{
    /// <summary>
    /// Obtiene todos los lotes de inventario.
    /// </summary>
    Task<IReadOnlyList<InventoryLot>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene los lotes que se van a modificar dentro de una operación.
    /// </summary>
    Task<IReadOnlyList<InventoryLot>> ListForUpdateAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Busca un lote por su identificador.
    /// </summary>
    Task<InventoryLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Añade un lote para su persistencia.
    /// </summary>
    Task AddAsync(InventoryLot lot, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
