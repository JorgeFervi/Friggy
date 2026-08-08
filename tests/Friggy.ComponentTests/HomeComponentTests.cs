using Bunit;
using Friggy.ComponentTests.Testing;

namespace Friggy.ComponentTests;

public sealed class HomeComponentTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void Render_DefaultState_ShowsCatalogNavigation()
    {
        var component = Render<global::Friggy.Web.Components.Pages.Home>();

        Assert.Equal("Friggy", component.Find("h1").TextContent);
        Assert.Collection(
            component.FindAll("nav a"),
            link => Assert.Equal("ingredients", link.GetAttribute("href")),
            link => Assert.Equal("unit-types", link.GetAttribute("href")),
            link => Assert.Equal("recipe-tags", link.GetAttribute("href")),
            link => Assert.Equal("meal-types", link.GetAttribute("href")));
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task ApiClient_QueuedResponse_ReturnsConfiguredPayload()
    {
        Api.RespondWith("application/json", "{\"status\":\"ready\"}");

        var payload = await ApiClient.GetStringAsync(
            "/api/status",
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("{\"status\":\"ready\"}", payload);
        Assert.Equal(new Uri("http://localhost/api/status"), Api.LastRequest?.RequestUri);
    }
}
