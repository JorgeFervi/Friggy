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

    [Fact]
    [Trait("Category", "Component")]
    public void ShoppingList_AllCovered_KeepsAllFilterAvailable()
    {
        var api = new StubShoppingListApiClient
        {
            Response = new ShoppingListResponse(
                new DateOnly(2026, 8, 22),
                new DateOnly(2026, 8, 22),
                new DateOnly(2026, 8, 22),
                [new(Guid.NewGuid(), "Tomate", Guid.NewGuid(), "Gramo", "g", 1.25m, 2m, 0m)]),
        };
        Services.AddSingleton<IShoppingListApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.ShoppingList>();

        component.Find("form").Submit();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Todo está cubierto", component.Markup, StringComparison.Ordinal);
            Assert.Single(component.FindAll("button"), button => button.TextContent.Contains("Todos", StringComparison.Ordinal));
        });

        component.FindAll("button").Single(button => button.TextContent.Contains("Todos", StringComparison.Ordinal)).Click();
        component.WaitForAssertion(() => Assert.Contains("Tomate", component.Markup, StringComparison.Ordinal));
    }

    private sealed class StubShoppingListApiClient : IShoppingListApiClient
    {
        public bool WasCalled { get; private set; }
        public ShoppingListResponse? Response { get; init; }
        public Task<ShoppingListResponse> GetAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(Response ?? new ShoppingListResponse(from, endDate, from, []));
        }
    }
}
