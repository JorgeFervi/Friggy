using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class CatalogVisualJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    private static readonly ViewportSize Mobile = new() { Width = 360, Height = 800 };
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 1000 };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task RecipeTags_PopulatedCatalog_MatchesResponsiveBaselines()
    {
        const string tagName = "Cocina de temporada";
        var failures = new List<Exception>();

        await RunScenarioAsync(async () =>
        {
            await fixture.ResetRecipeTagDataAsync(TestContext.Current.CancellationToken);
            try
            {
                await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
                await NavigateToInteractivePageAsync("/recipe-tags");
                await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(tagName);
                await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
                await Expect(Page.Locator(".friggy-catalog-card").Filter(new() { HasText = tagName })).ToBeVisibleAsync();

                await AssertTouchTargetAsync(Page.GetByRole(AriaRole.Button, new() { Name = $"Editar {tagName}", Exact = true }));
                await CaptureAsync("recipe-tags-mobile-360x800", failures);
                await AssertNoHorizontalOverflowAsync();

                await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
                await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = tagName, Exact = true })).ToBeVisibleAsync();
                await CaptureAsync("recipe-tags-desktop-1440x1000", failures);
                await AssertNoHorizontalOverflowAsync();

                if (failures.Count > 0)
                {
                    throw new AggregateException("Las capturas visuales del catálogo requieren revisión.", failures);
                }
            }
            finally
            {
                await fixture.ResetRecipeTagDataAsync(TestContext.Current.CancellationToken);
            }
        });
    }

    private static async Task AssertTouchTargetAsync(ILocator locator)
    {
        var bounds = await locator.BoundingBoxAsync();
        Assert.NotNull(bounds);
        Assert.True(bounds.Width >= 44, $"El control táctil debe medir al menos 44 px de ancho y mide {bounds.Width}.");
        Assert.True(bounds.Height >= 44, $"El control táctil debe medir al menos 44 px de alto y mide {bounds.Height}.");
    }

    private async Task AssertNoHorizontalOverflowAsync()
    {
        var hasOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
        Assert.False(hasOverflow, "El catálogo no debe tener overflow horizontal.");
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
