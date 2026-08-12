using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence;

public sealed class InventoryUnitOfWork(FriggyDbContext context) : IInventoryUnitOfWork
{
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
