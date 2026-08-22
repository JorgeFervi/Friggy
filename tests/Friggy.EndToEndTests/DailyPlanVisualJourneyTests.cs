using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class DailyPlanVisualJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    private static readonly ViewportSize Mobile = new() { Width = 360, Height = 800 };
    private static readonly ViewportSize Tablet = new() { Width = 768, Height = 1024 };
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 1000 };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task DailyPlanning_ListEditorAndCompletion_MatchResponsiveBaselines()
    {
        const string ingredientName = "Ingrediente visual diario";
        const string recipeName = "Crema visual diaria";
        var failures = new List<Exception>();

        await RunScenarioAsync(async () =>
        {
            await fixture.ResetPlanningDataAsync(TestContext.Current.CancellationToken);
            await fixture.ResetRecipeDataAsync(TestContext.Current.CancellationToken);
            await EnsureIngredientAsync(ingredientName);
            await CreateRecipeAsync(ingredientName, recipeName);

            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await OpenDayRangeAsync("2030-08-05");
            await CaptureAsync("daily-plans-list-mobile-360x800", failures);
            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await CaptureAsync("daily-plans-list-desktop-1440x1000", failures);

            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Planificar este día", Exact = true }).ClickAsync();
            var lunch = Page.Locator("[data-testid='meal-slot']")
                .Filter(new LocatorFilterOptions { HasText = "Comida" });
            await lunch.GetByLabel("Receta", new() { Exact = true })
                .SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await CaptureAsync("daily-plan-editor-mobile-360x800", failures);
            await AssertNoHorizontalOverflowAsync();
            await Page.SetViewportSizeAsync(Tablet.Width, Tablet.Height);
            await CaptureAsync("daily-plan-editor-tablet-768x1024", failures);
            await AssertNoHorizontalOverflowAsync();
            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await CaptureAsync("daily-plan-editor-desktop-1440x1000", failures);
            await AssertNoHorizontalOverflowAsync();

            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await lunch.GetByRole(AriaRole.Button, new() { Name = "Completar", Exact = true }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Selecciona los consumos", Exact = true })).ToBeVisibleAsync();
            await CaptureAsync("daily-plan-completion-mobile-360x800", failures);

            if (failures.Count > 0)
            {
                throw new AggregateException("Las capturas visuales de planificación diaria requieren revisión.", failures);
            }
        });
    }

    private async Task OpenDayRangeAsync(string date)
    {
        await NavigateToInteractivePageAsync("/daily-plans");
        await Page.GetByLabel("Desde", new() { Exact = true }).FillAsync(date);
        await Page.GetByLabel("Hasta", new() { Exact = true }).FillAsync(date);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Consultar", Exact = true }).ClickAsync();
    }

    private async Task EnsureIngredientAsync(string ingredientName)
    {
        await NavigateToInteractivePageAsync("/ingredients");
        if (await Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true }).CountAsync() == 0)
        {
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
        }
    }

    private async Task CreateRecipeAsync(string ingredientName, string recipeName)
    {
        await NavigateToInteractivePageAsync("/recipes/new");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
        await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("35");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true }).ClickAsync();
        await Page.GetByLabel("Ingrediente", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("2");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true }).ClickAsync();
        await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Preparar y servir templado");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true }).ClickAsync();
    }

    private async Task AssertNoHorizontalOverflowAsync()
    {
        var hasOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasOverflow, "La página de planificación diaria no debe tener overflow horizontal.");
    }

    private async Task CaptureAsync(string name, List<Exception> failures)
    {
        try
        {
            await VisualSnapshot.AssertAsync(Page, name, TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            failures.Add(exception);
        }
    }
}
