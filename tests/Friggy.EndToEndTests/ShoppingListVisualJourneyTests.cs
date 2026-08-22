using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class ShoppingListVisualJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    private static readonly ViewportSize Mobile = new() { Width = 360, Height = 800 };
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 1000 };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task ShoppingList_EmptyRange_MatchesResponsiveBaselines()
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var failures = new List<Exception>();
        await RunScenarioAsync(async () =>
        {
            await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
            await NavigateToInteractivePageAsync("/shopping-list");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Calcular lista", Exact = true }).ClickAsync();
            await Expect(Page.GetByText("No hay comidas planificadas", new() { Exact = true })).ToBeVisibleAsync();
            await CaptureAsync("shopping-list-empty-mobile-360x800", failures);

            await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
            await CaptureAsync("shopping-list-empty-desktop-1440x1000", failures);
            var hasOverflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(hasOverflow, "La lista de la compra no debe tener overflow horizontal en escritorio.");
            if (failures.Count > 0)
            {
                throw new AggregateException("Las capturas visuales de la lista de la compra requieren revisión.", failures);
            }
        });
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
