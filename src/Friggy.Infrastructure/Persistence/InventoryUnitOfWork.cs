using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence;

/// <summary>
/// Unidad de trabajo que persiste operaciones de inventario y traduce los
/// conflictos de concurrencia a errores de aplicación.
/// </summary>
public sealed class InventoryUnitOfWork(FriggyDbContext context) : IInventoryUnitOfWork
{
    /// <summary>
    /// Persiste los cambios pendientes de una operación de inventario.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token para cancelar la operación de persistencia.
    /// </param>
    /// <exception cref="InventoryConflictException">
    /// Excepción lanzada cuando el inventario ha cambiado durante la operación.
    /// </exception>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConflictException(
                "inventory-lot.concurrency",
                "El inventario ha cambiado. Actualiza los datos e inténtalo de nuevo.");
        }
    }
}
