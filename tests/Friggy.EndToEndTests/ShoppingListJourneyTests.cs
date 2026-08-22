using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class ShoppingListJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_CalculatesShoppingList_FromPlannedMealsAndAvailableInventory()
    {
        ArgumentNullException.ThrowIfNull(fixture);
        const string ingredientName = "Tomate E2E lista compra";
        const string recipeName = "Sopa E2E lista compra";
        await RunScenarioAsync(async () =>
        {
            await CreateRecipeAsync(ingredientName, recipeName);
            await AddInventoryAsync(ingredientName);
            await CreateDailyPlanAsync(recipeName, "2030-01-06");

            await NavigateToInteractivePageAsync("/shopping-list");
            await Page.GetByLabel("Fecha inicial", new() { Exact = true }).FillAsync("2030-01-06");
            await Page.GetByLabel("Fecha final", new() { Exact = true }).FillAsync("2030-01-06");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Calcular lista", Exact = true }).ClickAsync();

            var row = Page.GetByRole(AriaRole.Row).Filter(new LocatorFilterOptions { HasText = ingredientName });
            await Expect(row).ToContainTextAsync("1 g");
            await Expect(row).ToContainTextAsync("2 g");
            await Expect(row).ToContainTextAsync("Cubierto");
        });
    }

    private async Task AddInventoryAsync(string ingredientName)
    {
        await NavigateToInteractivePageAsync("/inventory");
        await Page.GetByLabel("Ingrediente", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("2");
        await Page.GetByLabel("Caducidad", new() { Exact = true }).FillAsync("2030-12-31");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir lote", Exact = true }).ClickAsync();
        await Expect(Page.GetByText(ingredientName, new() { Exact = true }).First).ToBeVisibleAsync();
    }

    private async Task CreateDailyPlanAsync(string recipeName, string date)
    {
        await NavigateToInteractivePageAsync("/daily-plans");
        await Page.GetByLabel("Desde", new() { Exact = true }).FillAsync(date);
        await Page.GetByLabel("Hasta", new() { Exact = true }).FillAsync(date);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Consultar", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Planificar este día", Exact = true }).ClickAsync();
        var lunch = Page.Locator("[data-testid='meal-slot']").Filter(new LocatorFilterOptions { HasText = "Comida" });
        await lunch.GetByLabel("Receta", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = recipeName });
        await Expect(Page.GetByText("Cambios guardados.", new() { Exact = true })).ToBeVisibleAsync();
    }

    private async Task CreateRecipeAsync(string ingredientName, string recipeName)
    {
        await NavigateToInteractivePageAsync("/ingredients");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
        await NavigateToInteractivePageAsync("/recipes/new");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
        await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("25");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true }).ClickAsync();
        await Page.GetByLabel("Ingrediente", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true }).ClickAsync();
        await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Preparar");
        await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true })).ToBeVisibleAsync();
    }
}
