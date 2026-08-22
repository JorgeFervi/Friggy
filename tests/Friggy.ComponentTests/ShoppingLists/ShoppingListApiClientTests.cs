using System.Text.Json;
using Friggy.Application.ShoppingLists.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;

namespace Friggy.ComponentTests.ShoppingLists;

public sealed class ShoppingListApiClientTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public async Task GetAsync_UsesInclusiveDatesInDedicatedRoute()
    {
        var response = new ShoppingListResponse(new(2030, 1, 6), new(2030, 1, 12), new(2030, 1, 5), []);
        Api.RespondWith("application/json", JsonSerializer.Serialize(response));
        await new ShoppingListApiClient(ApiClient).GetAsync(response.From, response.To, TestContext.Current.CancellationToken);
        var request = Assert.Single(Api.Requests);
        Assert.Equal("api/shopping-list?from=2030-01-06&to=2030-01-12", request.Uri?.PathAndQuery.TrimStart('/'));
    }
}
