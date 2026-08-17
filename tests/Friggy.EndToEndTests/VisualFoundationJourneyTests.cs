using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class VisualFoundationJourneyTests : FriggyPageTest
{
    private static readonly (string Name, ViewportSize Viewport)[] Viewports =
    [
        ("mobile-360x800", new ViewportSize { Width = 360, Height = 800 }),
        ("tablet-768x1024", new ViewportSize { Width = 768, Height = 1024 }),
        ("desktop-1440x1000", new ViewportSize { Width = 1440, Height = 1000 }),
    ];

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task HomePage_ApprovedViewports_MatchesVisualFoundation()
    {
        await RunScenarioAsync(async () =>
        {
            foreach (var (name, viewport) in Viewports)
            {
                await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
                await NavigateToInteractivePageAsync("/");
                await VisualSnapshot.AssertAsync(
                    Page,
                    $"home-{name}",
                    TestContext.Current.CancellationToken);
            }
        });
    }
}
