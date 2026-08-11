using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class RecipeJourneyTests : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task RecipeJourney_CreateNavigateAndDelete_PersistsExpectedBrowserState()
    {
        const string ingredientName = "Tomate E2E receta";
        const string recipeName = "Gazpacho E2E receta";

        await RunScenarioAsync(async () =>
        {
            await NavigateToInteractivePageAsync("/ingredients");
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true }))
                .ToBeVisibleAsync();

            await NavigateToInteractivePageAsync("/recipes/new");
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
            await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("20");

            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true })
                .ClickAsync();
            await Page.GetByLabel("Ingrediente", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
            await Page.GetByLabel("Unidad", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
            await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("1.5");

            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true })
                .ClickAsync();
            await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Triturar y servir");
            await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true })
                .ClickAsync();

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true }))
                .ToBeVisibleAsync();
            await Expect(Page.GetByText(ingredientName, new() { Exact = false })).ToBeVisibleAsync();
            await Expect(Page.GetByText("Triturar y servir", new() { Exact = false })).ToBeVisibleAsync();

            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Principal", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Recetas", Exact = true })
                .ClickAsync();
            var recipeRow = Page.GetByRole(AriaRole.Row)
                .Filter(new LocatorFilterOptions { HasText = recipeName });
            await Expect(recipeRow).ToBeVisibleAsync();
            Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await recipeRow.GetByRole(AriaRole.Button, new() { Name = "Borrar", Exact = true })
                .ClickAsync();

            await Expect(recipeRow)
                .Not.ToBeVisibleAsync();

            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Principal", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Planes semanales", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Planes semanales", Exact = true }))
                .ToBeVisibleAsync();

            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Principal", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Catálogos", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Catálogos", Exact = true }))
                .ToBeVisibleAsync();
        });
    }
}
