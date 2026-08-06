using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Friggy.IntegrationTests.Api;

public sealed class HealthEndpointTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetHealth_ComposedApplication_ReturnsHealthy()
    {
        await using var factory = new WebApplicationFactory<global::Friggy.Api.AssemblyMarker>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", content);
    }
}
