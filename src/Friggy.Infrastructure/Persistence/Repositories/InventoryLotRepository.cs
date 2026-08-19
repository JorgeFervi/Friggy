using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core para consultar y persistir lotes de inventario, incluidos
/// sus movimientos.
/// </summary>
public sealed class InventoryLotRepository(FriggyDbContext context)
    : IInventoryLotRepository
{
    /// <summary>
    /// Obtiene todos los lotes con sus movimientos ordenados.
    /// </summary>
    public async Task<IReadOnlyList<InventoryLot>> ListAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Busca un lote con sus movimientos por su identificador.
    /// </summary>
    public Task<InventoryLot?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(lot => lot.Id == id, cancellationToken);

    /// <summary>
    /// Obtiene los lotes con seguimiento para operaciones que los modificarán.
    /// </summary>
    public async Task<IReadOnlyList<InventoryLot>> ListForUpdateAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery().ToListAsync(cancellationToken);

    /// <summary>
    /// Añade un lote al contexto.
    /// </summary>
    public async Task AddAsync(
        InventoryLot lot,
        CancellationToken cancellationToken) =>
        await context.InventoryLots.AddAsync(lot, cancellationToken);

    /// <summary>
    /// Persiste los cambios y traduce los conflictos de concurrencia a un error
    /// de aplicación.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InventoryConflictException(
                "inventory-lot.concurrency",
                "El lote ha cambiado desde que se cargó. Actualiza los datos e inténtalo de nuevo.")
            {
                Source = exception.Source,
            };
        }
    }

    private IQueryable<InventoryLot> CompleteQuery() =>
        context.InventoryLots
            .Include(lot => lot.Movements.OrderBy(movement => movement.OccurredAt));
}
