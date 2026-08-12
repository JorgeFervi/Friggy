using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class InventoryLotRepository(FriggyDbContext context)
    : IInventoryLotRepository
{
    public async Task<IReadOnlyList<InventoryLot>> ListAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cancellationToken);

    public Task<InventoryLot?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(lot => lot.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InventoryLot>> ListForUpdateAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery().ToListAsync(cancellationToken);

    public async Task AddAsync(
        InventoryLot lot,
        CancellationToken cancellationToken) =>
        await context.InventoryLots.AddAsync(lot, cancellationToken);

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
