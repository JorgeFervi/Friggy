using Friggy.Application.ShoppingLists.Dtos;

namespace Friggy.Web.Api;

public interface IShoppingListApiClient
{
    Task<ShoppingListResponse> GetAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken);
}
