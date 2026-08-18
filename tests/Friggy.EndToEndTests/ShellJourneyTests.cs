using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class ShellJourneyTests : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Shell_DesktopAndCompactViewports_ExposeExpectedNavigationBehavior()
    {
        await RunScenarioAsync(async () =>
        {
            await Page.SetViewportSizeAsync(1440, 1000);
            await NavigateToInteractivePageAsync("/");
            await Expect(Page.Locator("[data-testid='desktop-sidebar']")).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Abrir menú", Exact = true }))
                .ToBeHiddenAsync();

            await Page.SetViewportSizeAsync(768, 1024);
            var menuButton = Page.GetByRole(AriaRole.Button, new() { Name = "Abrir menú", Exact = true });
            await Expect(Page.Locator("[data-testid='desktop-sidebar']")).ToBeHiddenAsync();
            await Expect(menuButton).ToBeVisibleAsync();

            await menuButton.ClickAsync();
            var drawer = Page.Locator("#navigation-drawer");
            await Expect(drawer).ToHaveAttributeAsync("aria-hidden", "false");
            await Expect(menuButton).ToHaveAttributeAsync("aria-expanded", "true");
            Assert.Equal("hidden", await Page.EvaluateAsync<string>("getComputedStyle(document.body).overflow"));
            Assert.Null(await drawer.GetAttributeAsync("inert"));
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Cerrar menú", Exact = true }))
                .ToBeFocusedAsync();

            await Page.Keyboard.PressAsync("Escape");
            await Expect(drawer).ToHaveAttributeAsync("aria-hidden", "true");
            await Expect(menuButton).ToBeFocusedAsync();
            Assert.NotEqual("hidden", await Page.EvaluateAsync<string>("getComputedStyle(document.body).overflow"));

            await menuButton.ClickAsync();
            await Page.GetByRole(AriaRole.Navigation, new() { Name = "Principal", Exact = true })
                .GetByRole(AriaRole.Link, new() { Name = "Recetas", Exact = true })
                .ClickAsync();
            await Expect(drawer).ToHaveAttributeAsync("aria-hidden", "true");
            Assert.EndsWith("/recipes", Page.Url, StringComparison.Ordinal);
        });
    }
}
