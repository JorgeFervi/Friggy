using Bunit;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Catalogs;

public sealed class CatalogPagesTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void UnitTypes_ApiReturnsUnit_RendersNameAndSymbol()
    {
        Api.RespondWith("application/json", "[{\"id\":\"10000000-0000-0000-0000-000000000001\",\"name\":\"Gramo\",\"symbol\":\"g\"}]");
        Services.AddSingleton<IUnitTypesApiClient>(new CatalogApiClient(ApiClient));

        var component = Render<global::Friggy.Web.Components.Pages.UnitTypes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Gramo", component.Markup, StringComparison.Ordinal);
            Assert.Contains(">g<", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeTags_ApiReturnsEmpty_ShowsEmptyState()
    {
        Api.RespondWith("application/json", "[]");
        Services.AddSingleton<IRecipeTagsApiClient>(new CatalogApiClient(ApiClient));

        var component = Render<global::Friggy.Web.Components.Pages.RecipeTags>();

        component.WaitForAssertion(() =>
            Assert.Contains("No hay etiquetas", component.Markup, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MealTypes_ApiReturnsMealType_RendersOrderedValue()
    {
        Api.RespondWith("application/json", "[{\"id\":\"20000000-0000-0000-0000-000000000003\",\"name\":\"Cena\",\"order\":2}]");
        Services.AddSingleton<IMealTypesApiClient>(new CatalogApiClient(ApiClient));

        var component = Render<global::Friggy.Web.Components.Pages.MealTypes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Cena", component.Markup, StringComparison.Ordinal);
            Assert.Contains(">2<", component.Markup, StringComparison.Ordinal);
        });
    }
}
