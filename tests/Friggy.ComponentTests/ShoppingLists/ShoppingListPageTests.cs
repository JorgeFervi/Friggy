using Friggy.Application.ShoppingLists.Dtos;
using Bunit;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.ShoppingLists;

public sealed class ShoppingListPageTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void ShoppingList_ShowsDateRangeAndDoesNotLoadUntilRequested()
    {
        var api = new StubShoppingListApiClient();
        Services.AddSingleton<IShoppingListApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.ShoppingList>();
        Assert.Equal("Lista de la compra", component.Find("h1").TextContent);
        Assert.NotNull(component.Find("#shopping-list-from"));
        Assert.NotNull(component.Find("#shopping-list-to"));
        Assert.False(api.WasCalled);
    }

    private sealed class StubShoppingListApiClient : IShoppingListApiClient
    {
        public bool WasCalled { get; private set; }
        public Task<ShoppingListResponse> GetAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(new ShoppingListResponse(from, endDate, from, []));
        }
    }
}
