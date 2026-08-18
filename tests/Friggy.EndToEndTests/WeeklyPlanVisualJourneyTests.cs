using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class WeeklyPlanVisualJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    private static readonly ViewportSize Mobile = new() { Width = 360, Height = 800 };
    private static readonly ViewportSize Tablet = new() { Width = 768, Height = 1024 };
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 1000 };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task WeeklyPlanning_ListCalendarAndCompletion_MatchResponsiveBaselines()
    {
        const string ingredientName = "Ingrediente visual planificación";
        const string recipeName = "Crema visual planificación";
        const string planName = "Semana visual de agosto";
        var failures = new List<Exception>();

        await RunScenarioAsync(async () =>
        {
            await fixture.ResetPlanningDataAsync(TestContext.Current.CancellationToken);
            await fixture.ResetRecipeDataAsync(TestContext.Current.CancellationToken);
            await EnsureIngredientAsync(ingredientName);
            await CreateRecipeAsync(ingredientName, recipeName);

            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await NavigateToInteractivePageAsync("/weekly-plans");
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(planName);
            await Page.GetByLabel("Lunes de inicio", new() { Exact = true }).FillAsync("2030-08-05");
            await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Menú familiar con platos sencillos para toda la semana.");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Crear plan", Exact = true }).ClickAsync();

            var monday = Page.Locator("section[data-testid='weekly-plan-day']").First;
            var lunch = monday.Locator("[data-testid='meal-slot']").Filter(new LocatorFilterOptions { HasText = "Comida" });
            await lunch.GetByLabel("Receta", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = recipeName });
            await Expect(Page.Locator(".friggy-weekly-plan-details__status")).ToHaveTextAsync("Asignación guardada.");

            await CaptureAsync("weekly-plan-calendar-mobile-360x800", failures);
            await AssertNoHorizontalOverflowAsync();
            await Page.SetViewportSizeAsync(Tablet.Width, Tablet.Height);
            await CaptureAsync("weekly-plan-calendar-tablet-768x1024", failures);
            await AssertNoHorizontalOverflowAsync();
            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await CaptureAsync("weekly-plan-calendar-desktop-1440x1000", failures);
            await AssertNoHorizontalOverflowAsync();

            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await lunch.GetByRole(AriaRole.Button, new() { Name = "Completar", Exact = true }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Selecciona los consumos", Exact = true })).ToBeVisibleAsync();
            await CaptureAsync("weekly-plan-completion-mobile-360x800", failures);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Cancelar", Exact = true }).ClickAsync();

            await Page.GetByRole(AriaRole.Link, new() { Name = "Volver a planes semanales", Exact = true }).ClickAsync();
            await Expect(Page.GetByText(planName, new() { Exact = true })).ToBeVisibleAsync();
            await CaptureAsync("weekly-plans-list-mobile-360x800", failures);
            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await CaptureAsync("weekly-plans-list-desktop-1440x1000", failures);

            if (failures.Count > 0)
            {
                throw new AggregateException("Las capturas visuales de planificación requieren revisión.", failures);
            }
        });
    }

    private async Task EnsureIngredientAsync(string ingredientName)
    {
        await NavigateToInteractivePageAsync("/ingredients");
        if (await Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true }).CountAsync() == 0)
        {
            await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
        }
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true })).ToBeVisibleAsync();
    }

    private async Task CreateRecipeAsync(string ingredientName, string recipeName)
    {
        await NavigateToInteractivePageAsync("/recipes/new");
        await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
        await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("35");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true }).ClickAsync();
        await Page.GetByLabel("Ingrediente", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("2");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true }).ClickAsync();
        await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Preparar y servir templado");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true })).ToBeVisibleAsync();
    }

    private async Task AssertNoHorizontalOverflowAsync()
    {
        var hasOverflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        if (hasOverflow)
        {
            var offenders = await Page.EvaluateAsync<string[]>(
                "() => [...document.querySelectorAll('*')].filter(element => element.getBoundingClientRect().right > document.documentElement.clientWidth + 1).slice(0, 8).map(element => `${element.tagName}.${element.className}: ${element.getBoundingClientRect().right}`)");
            Assert.Fail("La página de planificación no debe tener overflow horizontal. " + string.Join(" | ", offenders));
        }
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
