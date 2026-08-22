using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class DailyPlanJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_CreatesDailyAssignment_PersistsAfterServiceRestart()
    {
        const string ingredientName = "Calabacín E2E diario";
        const string recipeName = "Crema E2E diaria";

        await RunScenarioAsync(async () =>
        {
            await CreateRecipeAsync(ingredientName, recipeName);
            await OpenOrCreateDayAsync("2030-01-07");

            var lunch = MealSlot("Comida");
            await lunch.GetByLabel("Receta", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await Expect(Page.Locator(".friggy-daily-plan-details__status"))
                .ToHaveTextAsync("Cambios guardados.");

            var planPath = new Uri(Page.Url).PathAndQuery;
            await Page.GotoAsync("about:blank");
            await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);
            await NavigateToInteractivePageAsync(planPath);
            await Expect(MealSlot("Comida").GetByLabel("Receta", new() { Exact = true })
                .Locator("option:checked")).ToHaveTextAsync(recipeName);
        });
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_PlansIndependentDays_AndSkipsOnlyOneMeal()
    {
        const string ingredientName = "Tomate E2E días independientes";
        const string recipeName = "Gazpacho E2E días independientes";

        await RunScenarioAsync(async () =>
        {
            await CreateRecipeAsync(ingredientName, recipeName);
            await OpenOrCreateDayAsync("2030-01-07");
            await MealSlot("Comida").GetByLabel("Receta", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await MealSlot("Comida").GetByLabel("Motivo para omitir Comida", new() { Exact = true })
                .FillAsync("Viaje");
            await MealSlot("Comida").GetByRole(AriaRole.Button, new() { Name = "Omitir", Exact = true })
                .ClickAsync();
            await Expect(MealSlot("Comida").GetByText("Omitida", new() { Exact = true }))
                .ToBeVisibleAsync();

            await OpenOrCreateDayAsync("2030-01-08");
            var tuesdayLunch = MealSlot("Comida");
            await tuesdayLunch.GetByLabel("Receta", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await Expect(tuesdayLunch.GetByRole(AriaRole.Button, new() { Name = "Completar", Exact = true }))
                .ToBeVisibleAsync();
            await Expect(tuesdayLunch.GetByText("Omitida", new() { Exact = true }))
                .ToHaveCountAsync(0);
        });
    }

    private async Task OpenOrCreateDayAsync(string date)
    {
        await NavigateToInteractivePageAsync("/daily-plans");
        await Page.GetByLabel("Desde", new() { Exact = true }).FillAsync(date);
        await Page.GetByLabel("Hasta", new() { Exact = true }).FillAsync(date);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Consultar", Exact = true }).ClickAsync();
        var create = Page.GetByRole(AriaRole.Button, new() { Name = "Planificar este día", Exact = true });
        if (await create.CountAsync() > 0)
        {
            await create.ClickAsync();
        }
        else
        {
            await Page.GetByRole(AriaRole.Link, new() { Name = "Abrir plan", Exact = true }).ClickAsync();
        }

        await Expect(Page.Locator("[data-testid='daily-plan-editor']")).ToBeVisibleAsync();
    }

    private ILocator MealSlot(string mealTypeName) =>
        Page.Locator("[data-testid='meal-slot']")
            .Filter(new LocatorFilterOptions { HasText = mealTypeName });

    private async Task CreateRecipeAsync(string ingredientName, string recipeName)
    {
        await NavigateToInteractivePageAsync("/ingredients");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
        await NavigateToInteractivePageAsync("/recipes/new");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
        await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("25");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true }).ClickAsync();
        await Page.GetByLabel("Ingrediente", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true }).ClickAsync();
        await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Cocinar y triturar");
        await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true }).ClickAsync();
    }
}
