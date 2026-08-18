using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class VisualFoundationJourneyTests(FullStackFixture fixture) : FriggyPageTest
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
            await fixture.ResetRecipeDataAsync(TestContext.Current.CancellationToken);
            var visualFailures = new List<Exception>();

            foreach (var (name, viewport) in Viewports)
            {
                await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
                await NavigateToInteractivePageAsync("/");

                try
                {
                    await VisualSnapshot.AssertAsync(
                        Page,
                        $"home-{name}",
                        TestContext.Current.CancellationToken);
                }
                catch (InvalidOperationException exception)
                {
                    visualFailures.Add(exception);
                }
            }

            if (visualFailures.Count > 0)
            {
                throw new AggregateException(visualFailures);
            }
        });
    }
}
