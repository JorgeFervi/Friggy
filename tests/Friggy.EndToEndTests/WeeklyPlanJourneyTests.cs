using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class WeeklyPlanJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_CreatesCatalogRecipeAndWeeklyAssignment_PersistsAfterServiceRestart()
    {
        const string ingredientName = "Calabacín E2E principal";
        const string recipeName = "Crema E2E principal";
        const string planName = "Semana E2E principal";

        await RunScenarioAsync(async () =>
        {
            await CreateRecipeAsync(ingredientName, recipeName);

            await NavigateToInteractivePageAsync("/weekly-plans");
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(planName);
            await Page.GetByLabel("Lunes de inicio", new() { Exact = true })
                .FillAsync("2030-01-07");
            await Page.GetByLabel("Descripción", new() { Exact = true })
                .FillAsync("Plan semanal creado desde Playwright");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Crear plan", Exact = true })
                .ClickAsync();

            var monday = Page.Locator("section[data-testid='weekly-plan-day']").First;
            await Expect(monday.GetByRole(AriaRole.Heading))
                .ToContainTextAsync("Lunes, 7 de enero");
            var lunch = MealSlot(monday, "Comida").GetByLabel("Receta", new() { Exact = true });
            await lunch.SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await Expect(Page.GetByRole(AriaRole.Status))
                .ToHaveTextAsync("Asignación guardada.");

            var planPath = new Uri(Page.Url).PathAndQuery;
            await Page.GotoAsync("about:blank");
            await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);
            await NavigateToInteractivePageAsync(planPath);
            monday = Page.Locator("section[data-testid='weekly-plan-day']").First;
            lunch = MealSlot(monday, "Comida").GetByLabel("Receta", new() { Exact = true });
            await Expect(lunch.Locator("option:checked")).ToHaveTextAsync(recipeName);
        });
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_ConfiguresDifferentDays_SkipsOneMealAndCompletesOnlyTheOther()
    {
        const string ingredientName = "Tomate E2E planificación avanzada";
        const string recipeName = "Gazpacho E2E planificación avanzada";
        const string planName = "Semana E2E planificación avanzada";

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

            var days = Page.Locator("section[data-testid='weekly-plan-day']");
            var monday = days.Nth(0);
            var tuesday = days.Nth(1);
            await monday.GetByRole(AriaRole.Button, new() { Name = "Eliminar hueco Cena", Exact = true })
                .ClickAsync();
            await tuesday.GetByRole(AriaRole.Button, new() { Name = "Eliminar hueco Desayuno", Exact = true })
                .ClickAsync();
            await Expect(monday.Locator("[data-testid='meal-slot']")).ToHaveCountAsync(2);
            await Expect(tuesday.Locator("[data-testid='meal-slot']")).ToHaveCountAsync(2);

            var mondayLunch = MealSlot(monday, "Comida");
            var tuesdayLunch = MealSlot(tuesday, "Comida");
            await mondayLunch.GetByLabel("Receta", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await tuesdayLunch.GetByLabel("Receta", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await mondayLunch.GetByLabel("Hora prevista", new() { Exact = true }).FillAsync("14:00");
            await mondayLunch.GetByLabel("Hora prevista", new() { Exact = true }).PressAsync("Tab");
            await tuesdayLunch.GetByLabel("Hora prevista", new() { Exact = true }).FillAsync("14:30");
            await tuesdayLunch.GetByLabel("Hora prevista", new() { Exact = true }).PressAsync("Tab");

            await mondayLunch.GetByLabel("Motivo para omitir", new() { Exact = true })
                .FillAsync("Viaje");
            await mondayLunch.GetByLabel("Alternativa", new() { Exact = true })
                .FillAsync("Bocadillo");
            await mondayLunch.GetByRole(AriaRole.Button, new() { Name = "Omitir", Exact = true })
                .ClickAsync();
            await Expect(mondayLunch.GetByText("Omitida: Viaje", new() { Exact = true }))
                .ToBeVisibleAsync();

            await tuesdayLunch.GetByRole(AriaRole.Button, new() { Name = "Completar", Exact = true })
                .ClickAsync();
            await Page.GetByLabel(new System.Text.RegularExpressions.Regex(ingredientName)).FillAsync("1");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmar finalización", Exact = true })
                .ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Status))
                .ToHaveTextAsync("Comida completada e inventario actualizado.");

            await NavigateToInteractivePageAsync("/inventory");
            await Page.GetByRole(AriaRole.Link, new() { Name = ingredientName, Exact = true })
                .ClickAsync();
            await Expect(Page.GetByText(new System.Text.RegularExpressions.Regex(@"2([,.]0+)? g disponibles")))
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
        await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("25");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true })
            .ClickAsync();
        await Page.GetByLabel("Ingrediente", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true })
            .ClickAsync();
        await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Cocinar y triturar");
        await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true })
            .ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true }))
            .ToBeVisibleAsync();
    }

    private static ILocator MealSlot(ILocator day, string mealTypeName) =>
        day.Locator("[data-testid='meal-slot']")
            .Filter(new LocatorFilterOptions { HasText = mealTypeName });
}
