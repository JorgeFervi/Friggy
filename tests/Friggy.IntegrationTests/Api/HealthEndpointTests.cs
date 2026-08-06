using System.Net;
using Friggy.Infrastructure.Persistence;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.IntegrationTests.Api;

public sealed class HealthEndpointTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetHealth_ComposedApplication_ReturnsHealthy()
    {
        await using var factory = new FriggyApiFactory(Database.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FriggyDbContext>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", content);
        Assert.Equal(Database.DatabaseName, context.Database.GetDbConnection().Database);
    }
}
