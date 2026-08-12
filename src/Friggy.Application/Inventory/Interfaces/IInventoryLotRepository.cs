using Friggy.Domain.Inventory;

namespace Friggy.Application.Inventory.Interfaces;

public interface IInventoryLotRepository
{
    Task<IReadOnlyList<InventoryLot>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryLot>> ListForUpdateAsync(
        CancellationToken cancellationToken);

    Task<InventoryLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(InventoryLot lot, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
