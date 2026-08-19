namespace Friggy.Application.Inventory.Interfaces;

/// <summary>
/// Contrato de unidad de trabajo para persistir operaciones de inventario.
/// </summary>
public interface IInventoryUnitOfWork
{
    /// <summary>
    /// Persiste los cambios pendientes de la operación.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
