using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class InventoryVisualJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    private static readonly ViewportSize Mobile = new() { Width = 360, Height = 800 };
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 1000 };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task Inventory_ListAndDetail_MatchResponsiveBaselinesAndExposeEveryStatus()
    {
        const string ingredientName = "Tomate azul visual";
        var failures = new List<Exception>();

        await RunScenarioAsync(async () =>
        {
            await fixture.ResetInventoryDataAsync(TestContext.Current.CancellationToken);
            await EnsureIngredientAsync(ingredientName);

            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await NavigateToInteractivePageAsync("/inventory");
            await CreateLotAsync(ingredientName, "5", "2030-01-20");
            await CreateLotAsync(ingredientName, "2", "2020-01-20");
            await CreateLotAsync(ingredientName, "3", "2031-01-20");

            var lotToExhaust = Page.GetByRole(AriaRole.Row)
                .Filter(new LocatorFilterOptions { HasText = "20/01/2031" });
            await lotToExhaust.GetByRole(AriaRole.Link, new() { Name = ingredientName, Exact = true }).ClickAsync();
            await Page.GetByLabel("Cantidad (g)", new() { Exact = true }).FillAsync("0");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Ajustar a cantidad real", Exact = true }).ClickAsync();
            await Expect(Page.Locator(".friggy-inventory-details__operation-status")).ToHaveTextAsync("Cantidad ajustada.");

            await NavigateToInteractivePageAsync("/inventory");
            await Page.GetByLabel("Mostrar agotados y caducados", new() { Exact = true }).CheckAsync();
            await Expect(VisibleStatus("Disponible")).ToBeVisibleAsync();
            await Expect(VisibleStatus("Agotado")).ToBeVisibleAsync();
            await Expect(VisibleStatus("Caducado")).ToBeVisibleAsync();
            await CaptureAsync("inventory-list-desktop-1440x1000", failures);
            await AssertNoHorizontalOverflowAsync();

            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await CaptureAsync("inventory-list-mobile-360x800", failures);
            await AssertNoHorizontalOverflowAsync();

            var availableCard = Page.Locator("[data-testid='inventory-lot-item']")
                .Filter(new LocatorFilterOptions { HasText = "20/01/2030" });
            await availableCard.GetByRole(AriaRole.Link, new() { Name = ingredientName, Exact = true }).ClickAsync();
            await Page.WaitForURLAsync("**/inventory/*");
            await Expect(Page.Locator("h1")).ToHaveTextAsync(ingredientName);
            await AssertTouchTargetAsync(Page.GetByRole(AriaRole.Button, new() { Name = "Consumir", Exact = true }));
            await AssertTouchTargetAsync(Page.GetByRole(AriaRole.Button, new() { Name = "Descartar", Exact = true }));
            await CaptureAsync("inventory-detail-mobile-360x800", failures);
            await AssertNoHorizontalOverflowAsync();
            await Page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
            Assert.True(await Page.EvaluateAsync<double>("() => window.scrollY") > 0);

            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await CaptureAsync("inventory-detail-desktop-1440x1000", failures);
            await AssertNoHorizontalOverflowAsync();

            if (failures.Count > 0)
            {
                throw new AggregateException("Las capturas visuales de inventario requieren revisión.", failures);
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

    private async Task CreateLotAsync(string ingredientName, string quantity, string expirationDate)
    {
        await Page.GetByLabel("Ingrediente", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await Page.GetByLabel("Unidad", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
        await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync(quantity);
        await Page.GetByLabel("Caducidad", new() { Exact = true }).FillAsync(expirationDate);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir lote", Exact = true }).ClickAsync();
        await Expect(Page.GetByLabel("Ingrediente", new() { Exact = true })).ToHaveValueAsync(string.Empty);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Añadir lote", Exact = true })).ToBeEnabledAsync();
    }

    private static async Task AssertTouchTargetAsync(ILocator locator)
    {
        var bounds = await locator.BoundingBoxAsync();
        Assert.NotNull(bounds);
        Assert.True(bounds.Width >= 44, $"El control táctil debe medir al menos 44 px de ancho y mide {bounds.Width}.");
        Assert.True(bounds.Height >= 44, $"El control táctil debe medir al menos 44 px de alto y mide {bounds.Height}.");
    }

    private ILocator VisibleStatus(string label) =>
        Page.Locator(".status-badge:visible").Filter(new LocatorFilterOptions { HasText = label });

    private async Task AssertNoHorizontalOverflowAsync()
    {
        var hasOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        if (hasOverflow)
        {
            var offenders = await Page.EvaluateAsync<string[]>(
                "() => [...document.querySelectorAll('*')].filter(element => element.getBoundingClientRect().right > document.documentElement.clientWidth + 1).slice(0, 8).map(element => element.tagName + '.' + element.className + ': ' + element.getBoundingClientRect().right)");
            Assert.Fail("La página de inventario no debe tener overflow horizontal. " + string.Join(" | ", offenders));
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
