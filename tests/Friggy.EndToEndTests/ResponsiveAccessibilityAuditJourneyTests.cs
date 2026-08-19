using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class ResponsiveAccessibilityAuditJourneyTests : FriggyPageTest
{
    private static readonly string[] PublicRoutes =
    [
        "/",
        "/recipes",
        "/recipes/new",
        "/weekly-plans",
        "/inventory",
        "/ingredients",
        "/unit-types",
        "/recipe-tags",
        "/meal-types",
        "/not-found",
        "/Error",
    ];

    private static readonly ViewportSize[] Viewports =
    [
        new() { Width = 360, Height = 800 },
        new() { Width = 768, Height = 1024 },
        new() { Width = 1440, Height = 1000 },
    ];

    [Fact]
    [Trait("Category", "E2E")]
    public async Task PublicRoutes_AllApprovedViewports_ReflowAndExposeAccessibleStructure()
    {
        await RunScenarioAsync(async () =>
        {
            foreach (var viewport in Viewports)
            {
                await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);

                foreach (var route in PublicRoutes)
                {
                    await NavigateToInteractivePageAsync(route);
                    await AssertAccessibleStructureAsync(route);
                    await AssertNoHorizontalOverflowAsync(route, $"{viewport.Width}x{viewport.Height}");
                }
            }

            await Page.SetViewportSizeAsync(1440, 1000);
            await NavigateToInteractivePageAsync("/recipes/new");
            await Page.EvaluateAsync("() => document.documentElement.style.zoom = '2'");
            await AssertNoHorizontalOverflowAsync("/recipes/new", "zoom 200 %");
        });
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task RuntimeVisualResources_AnyPublicRoute_RemainLocal()
    {
        var externalResources = new List<string>();
        Page.Request += (_, request) =>
        {
            if (request.ResourceType is "font" or "image" or "script" or "stylesheet" &&
                Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) &&
                uri.Host is not "127.0.0.1" and not "localhost")
            {
                externalResources.Add($"{request.ResourceType}: {request.Url}");
            }
        };

        await RunScenarioAsync(async () =>
        {
            foreach (var route in PublicRoutes)
            {
                await NavigateToInteractivePageAsync(route);
            }

            Assert.Empty(externalResources);
        });
    }

    private async Task AssertAccessibleStructureAsync(string route)
    {
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "es");
        await Expect(Page.GetByRole(AriaRole.Main)).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveCountAsync(1);

        var unnamedControls = await Page.Locator("button, input:not([type='hidden']), select, textarea")
            .EvaluateAllAsync<int>(
                "controls => controls.filter(control => !control.disabled && !(control.tagName === 'BUTTON' && control.textContent?.trim()) && !control.labels?.length && !control.getAttribute('aria-label') && !control.getAttribute('aria-labelledby')).length");
        Assert.True(unnamedControls == 0, $"La ruta '{route}' contiene {unnamedControls} controles sin nombre accesible.");
    }

    private async Task AssertNoHorizontalOverflowAsync(string route, string context)
    {
        var hasOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        if (!hasOverflow)
        {
            return;
        }

        var offenders = await Page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('*')].filter(element => element.getBoundingClientRect().right > document.documentElement.clientWidth + 1).slice(0, 8).map(element => `${element.tagName}.${element.className}: ${element.getBoundingClientRect().right}`)");
        Assert.Fail($"La ruta '{route}' tiene overflow horizontal en {context}. {string.Join(" | ", offenders)}");
    }
}
