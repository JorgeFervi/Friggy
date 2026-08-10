using Friggy.EndToEndTests.Testing;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class PlaywrightArtifactTests : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task RunScenarioAsync_Success_RemovesStaleFailureArtifacts()
    {
        const string scenarioName = nameof(RunScenarioAsync_Success_RemovesStaleFailureArtifacts);
        var artifactDirectory = GetArtifactDirectory(scenarioName);
        Directory.CreateDirectory(artifactDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(artifactDirectory, "stale.txt"),
            "stale",
            TestContext.Current.CancellationToken);

        await RunScenarioAsync(() => Task.CompletedTask, scenarioName);

        Assert.False(Directory.Exists(artifactDirectory));
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task RunScenarioAsync_Failure_PreservesDiagnosticArtifacts()
    {
        const string scenarioName = nameof(RunScenarioAsync_Failure_PreservesDiagnosticArtifacts);
        var artifactDirectory = GetArtifactDirectory(scenarioName);

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => RunScenarioAsync(
                    () => throw new InvalidOperationException("Fallo controlado."),
                    scenarioName));

            Assert.Equal("Fallo controlado.", exception.Message);
            Assert.True(File.Exists(Path.Combine(artifactDirectory, "failure.png")));
            Assert.True(File.Exists(Path.Combine(artifactDirectory, "trace.zip")));
            Assert.True(File.Exists(Path.Combine(artifactDirectory, "failure.webm")));
            Assert.True(File.Exists(Path.Combine(artifactDirectory, "browser.log")));
        }
        finally
        {
            if (Directory.Exists(artifactDirectory))
            {
                Directory.Delete(artifactDirectory, recursive: true);
            }
        }
    }
}
