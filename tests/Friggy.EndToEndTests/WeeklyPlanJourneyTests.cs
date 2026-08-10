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
            var lunch = monday.GetByLabel("Comida", new() { Exact = true });
            await lunch.SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await Expect(Page.GetByRole(AriaRole.Status))
                .ToHaveTextAsync("Asignación guardada.");

            var planPath = new Uri(Page.Url).PathAndQuery;
            await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);
            await NavigateToInteractivePageAsync(planPath);
            monday = Page.Locator("section[data-testid='weekly-plan-day']").First;
            lunch = monday.GetByLabel("Comida", new() { Exact = true });
            await Expect(lunch.Locator("option:checked")).ToHaveTextAsync(recipeName);
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
}
