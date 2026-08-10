using Friggy.EndToEndTests.Testing;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class FullStackFixtureSmokeTests(FullStackFixture fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task InitializeAsync_IsolatedEnvironment_ExposesHealthyApiAndWeb()
    {
        using var client = new HttpClient();

        using var apiResponse = await client.GetAsync(
            new Uri(fixture.ApiBaseUrl, "health"),
            TestContext.Current.CancellationToken);
        using var webResponse = await client.GetAsync(
            new Uri(fixture.WebBaseUrl, "health"),
            TestContext.Current.CancellationToken);

        Assert.True(apiResponse.IsSuccessStatusCode);
        Assert.True(webResponse.IsSuccessStatusCode);
        Assert.Equal(
            fixture.WebBaseUrl.AbsoluteUri,
            Environment.GetEnvironmentVariable("FRIGGY_WEB_BASE_URL"));
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task RestartServicesAsync_RunningEnvironment_ExposesHealthyApiAndWebAgain()
    {
        await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient();

        using var apiResponse = await client.GetAsync(
            new Uri(fixture.ApiBaseUrl, "health"),
            TestContext.Current.CancellationToken);
        using var webResponse = await client.GetAsync(
            new Uri(fixture.WebBaseUrl, "health"),
            TestContext.Current.CancellationToken);

        Assert.True(apiResponse.IsSuccessStatusCode);
        Assert.True(webResponse.IsSuccessStatusCode);
    }
}
