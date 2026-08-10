using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class ApiAvailabilityJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Ingredients_ApiUnavailable_ShowsActionableError()
    {
        await RunScenarioAsync(async () =>
        {
            await fixture.StopApiAsync(TestContext.Current.CancellationToken);

            try
            {
                await NavigateToInteractivePageAsync("/ingredients");

                var alert = Page.GetByRole(AriaRole.Alert);
                await Expect(alert).ToBeVisibleAsync();
                await Expect(alert).ToContainTextAsync("No se pudieron cargar los ingredientes");
                await Expect(Page.GetByText("No hay ingredientes. Añade el primero."))
                    .ToHaveCountAsync(0);
            }
            finally
            {
                await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);
            }
        });
    }
}
