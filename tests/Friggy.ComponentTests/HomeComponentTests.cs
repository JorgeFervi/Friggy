using Bunit;
using Friggy.ComponentTests.Testing;

namespace Friggy.ComponentTests;

public sealed class HomeComponentTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void Render_DefaultState_ShowsHeadingAndWelcomeMessage()
    {
        var component = Render<global::Friggy.Web.Components.Pages.Home>();

        component.MarkupMatches(
            """
            <h1>Hello, world!</h1>
            Welcome to your new app.
            """);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task ApiClient_QueuedResponse_ReturnsConfiguredPayload()
    {
        Api.RespondWith("application/json", "{\"status\":\"ready\"}");

        var payload = await ApiClient.GetStringAsync(
            "/api/status",
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("{\"status\":\"ready\"}", payload);
        Assert.Equal(new Uri("http://localhost/api/status"), Api.LastRequest?.RequestUri);
    }
}
