using Bunit;
using Friggy.ComponentTests.Testing;
using Microsoft.AspNetCore.Components;

namespace Friggy.ComponentTests.Layout;

public sealed class MainLayoutTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void Render_FromRecipesPage_ShowsPersistentMainNavigation()
    {
        var navigation = GetRequiredService<NavigationManager>();
        navigation.NavigateTo("recipes");

        var component = Render<global::Friggy.Web.Components.Layout.MainLayout>();

        Assert.EndsWith("/recipes", navigation.Uri, StringComparison.Ordinal);
        Assert.Collection(
            component.FindAll("nav[aria-label='Principal'] > a"),
            link =>
            {
                Assert.Equal("Inicio", link.TextContent.Trim());
                Assert.Equal("/", link.GetAttribute("href"));
            },
            link =>
            {
                Assert.Equal("Recetas", link.TextContent.Trim());
                Assert.Equal("/recipes", link.GetAttribute("href"));
            },
            link =>
            {
                Assert.Equal("Planes semanales", link.TextContent.Trim());
                Assert.Equal("/weekly-plans", link.GetAttribute("href"));
            },
            link =>
            {
                Assert.Equal("Inventario", link.TextContent.Trim());
                Assert.Equal("/inventory", link.GetAttribute("href"));
            },
            link =>
            {
                Assert.Equal("Catálogos", link.TextContent.Trim());
                Assert.Equal("/#catalogs", link.GetAttribute("href"));
            });
    }
}
