using System.Text.RegularExpressions;
using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class InventoryJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_CreatesLot_CompletesMealAndKeepsHistoryAfterRestart()
    {
        const string ingredientName = "Tomate inventario E2E";
        const string recipeName = "Tomate preparado E2E";
        const string planName = "Semana inventario E2E";

        await RunScenarioAsync(async () =>
        {
            await CreateRecipeAsync(ingredientName, recipeName);
            await NavigateToInteractivePageAsync("/inventory");
            await Page.GetByLabel("Ingrediente", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
            await Page.GetByLabel("Unidad", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
            await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("3");
            await Page.GetByLabel("Caducidad", new() { Exact = true }).FillAsync("2030-01-20");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir lote", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true }))
                .ToBeVisibleAsync();

            await NavigateToInteractivePageAsync("/weekly-plans");
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(planName);
            await Page.GetByLabel("Lunes de inicio", new() { Exact = true }).FillAsync("2030-01-07");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Crear plan", Exact = true })
                .ClickAsync();
            var monday = Page.Locator("section[data-testid='weekly-plan-day']").First;
            await monday.GetByLabel("Comida", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await monday.GetByLabel("Raciones", new() { Exact = true }).FillAsync("2");
            await monday.GetByLabel("Raciones", new() { Exact = true }).PressAsync("Tab");
            await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "2,000 g", Exact = true }))
                .ToBeVisibleAsync();

            await monday.GetByRole(AriaRole.Button, new() { Name = "Completar", Exact = true })
                .ClickAsync();
            await Page.GetByLabel(new Regex(ingredientName)).FillAsync("2");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmar finalización", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Status))
                .ToHaveTextAsync("Comida completada e inventario actualizado.");

            await Page.GotoAsync("about:blank");
            await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);
            await NavigateToInteractivePageAsync("/inventory");
            await Page.GetByRole(AriaRole.Link, new() { Name = ingredientName, Exact = true })
                .ClickAsync();
            await Expect(Page.GetByText(new Regex(@"1([,.]0+)? g disponibles"))).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Historial", Exact = true }))
                .ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Row)).ToHaveCountAsync(3);
        });
    }

    private async Task CreateRecipeAsync(string ingredientName, string recipeName)
    {
        await NavigateToInteractivePageAsync("/ingredients");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true }))
            .ToBeVisibleAsync();
        await NavigateToInteractivePageAsync("/recipes/new");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
        await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("10");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true })
            .ClickAsync();
        await Page.GetByLabel("Ingrediente", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true })
            .ClickAsync();
        await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Servir");
        await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true })
            .ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true }))
            .ToBeVisibleAsync();
    }
}
