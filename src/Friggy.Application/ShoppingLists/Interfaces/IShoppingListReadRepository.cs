using Friggy.Application.ShoppingLists.Dtos;

namespace Friggy.Application.ShoppingLists.Interfaces;

public interface IShoppingListReadRepository
{
    Task<ShoppingListSnapshot> GetSnapshotAsync(DateOnly from, DateOnly endDate, DateOnly today, CancellationToken cancellationToken);
}
