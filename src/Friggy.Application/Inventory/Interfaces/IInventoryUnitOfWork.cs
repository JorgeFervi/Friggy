namespace Friggy.Application.Inventory.Interfaces;

public interface IInventoryUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
