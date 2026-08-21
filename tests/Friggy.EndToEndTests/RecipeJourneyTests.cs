using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class RecipeJourneyTests : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task RecipeJourney_CreateAssociateEditReloadAndDelete_PersistsExpectedBrowserState()
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
            var step = Page.Locator("[data-testid='step-row']").First;
            await step.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Triturar y servir");
            await step.Locator("[data-testid='step-ingredients'] input[type='checkbox']").First.CheckAsync();
            await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true })
                .ClickAsync();

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true }))
                .ToBeVisibleAsync();
            await Expect(Page.GetByText("Triturar y servir", new() { Exact = false })).ToBeVisibleAsync();
            var associatedIngredients = Page.Locator("[data-testid='step-associated-ingredients']");
            await Expect(associatedIngredients).ToContainTextAsync(ingredientName);
            await Expect(associatedIngredients).ToContainTextAsync($"1,50 Gramo de {ingredientName}");

            await Page.GetByRole(AriaRole.Link, new() { Name = "Editar receta", Exact = true }).ClickAsync();
            await Page.Locator("[data-testid='interactive-ready']").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

            step = Page.Locator("[data-testid='step-row']").First;
            await Expect(step.Locator("[data-testid='step-ingredients'] input[type='checkbox']").First)
                .ToBeCheckedAsync();

            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true })
                .ClickAsync();
            var secondIngredientLine = Page.Locator("[data-testid='ingredient-row']").Nth(1);
            await secondIngredientLine.GetByLabel("Ingrediente", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
            await secondIngredientLine.GetByLabel("Unidad", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = "Unidad (ud)" });
            await secondIngredientLine.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("2");

            await step.Locator("[data-testid='step-ingredients'] input[type='checkbox']").Nth(1).CheckAsync();
            await step.GetByLabel("Descripción", new() { Exact = true })
                .FillAsync("Triturar, mezclar y servir");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true })
                .ClickAsync();

            await Expect(Page.GetByText("Triturar, mezclar y servir", new() { Exact = false }))
                .ToBeVisibleAsync();
            associatedIngredients = Page.Locator("[data-testid='step-associated-ingredients']");
            await Expect(associatedIngredients).ToContainTextAsync($"1,50 Gramo de {ingredientName}");
            await Expect(associatedIngredients).ToContainTextAsync($"2,00 Unidad de {ingredientName}");

            await Page.ReloadAsync();
            await Page.Locator("[data-testid='interactive-ready']").WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
            await Expect(Page.GetByText("Triturar, mezclar y servir", new() { Exact = false }))
                .ToBeVisibleAsync();
            associatedIngredients = Page.Locator("[data-testid='step-associated-ingredients']");
            await Expect(associatedIngredients).ToContainTextAsync($"1,50 Gramo de {ingredientName}");
            await Expect(associatedIngredients).ToContainTextAsync($"2,00 Unidad de {ingredientName}");

            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Principal", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Recetas", Exact = true })
                .ClickAsync();
            var recipeCard = Page.Locator("article[data-recipe-id]")
                .Filter(new LocatorFilterOptions { HasText = recipeName });
            await Expect(recipeCard).ToBeVisibleAsync();
            await recipeCard.GetByRole(AriaRole.Button, new() { Name = "Borrar", Exact = true })
                .ClickAsync();
            await Page.GetByRole(AriaRole.Dialog, new() { Name = "Borrar receta", Exact = true })
                .GetByRole(AriaRole.Button, new() { Name = "Borrar receta", Exact = true })
                .ClickAsync();

            await Expect(recipeCard)
                .Not.ToBeVisibleAsync();

            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Principal", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Planes semanales", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Planes semanales", Exact = true }))
                .ToBeVisibleAsync();

            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Catálogos", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Ingredientes", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Ingredientes", Exact = true }))
                .ToBeVisibleAsync();
        });
    }
}
