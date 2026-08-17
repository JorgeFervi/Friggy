namespace Friggy.IntegrationTests.Tooling;

public sealed class PowerShellScriptContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("setup.ps1")]
    [InlineData("start.ps1")]
    [InlineData("test.ps1")]
    [InlineData("quality-gate.ps1")]
    [InlineData("update-visual-baselines.ps1")]
    public void Script_Always_EnablesStrictFailFastExecution(string scriptName)
    {
        var script = ReadScript(scriptName);

        Assert.Contains("$ErrorActionPreference = 'Stop'", script, StringComparison.Ordinal);
        Assert.Contains("Set-StrictMode -Version Latest", script, StringComparison.Ordinal);
        Assert.Contains("common.ps1", script, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupScript_LocalBootstrap_OrchestratesRequiredIdempotentSteps()
    {
        var script = ReadScript("setup.ps1");

        AssertInOrder(
            script,
            "Assert-CommandAvailable -Name 'dotnet'",
            "Assert-CommandAvailable -Name 'docker'",
            "Assert-CommandAvailable -Name 'node'",
            "Assert-CommandAvailable -Name 'pnpm'",
            "pnpm install --frozen-lockfile",
            "docker compose up --detach --wait postgres",
            "dotnet tool restore",
            "dotnet restore Friggy.sln",
            "dotnet ef database update",
            "dotnet build tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj",
            "install chromium");
        Assert.DoesNotContain("docker compose down", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Remove-Item", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartScript_LocalServices_StartsHiddenProcessesAndWaitsForBothEndpoints()
    {
        var script = ReadScript("start.ps1");

        Assert.Contains("docker compose up --detach --wait postgres", script, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5292/health", script, StringComparison.Ordinal);
        Assert.Contains("http://localhost:5179", script, StringComparison.Ordinal);
        Assert.Contains("-WindowStyle Hidden", script, StringComparison.Ordinal);
        Assert.Contains("Wait-HttpEndpoint", script, StringComparison.Ordinal);
        Assert.Contains("'--environment'", script, StringComparison.Ordinal);
        Assert.Contains("'ASPNETCORE_ENVIRONMENT=Development'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("container_name", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestScript_AllSuites_BuildsOnceAndRunsInDefinedOrder()
    {
        var script = ReadScript("test.ps1");

        Assert.Equal(1, CountOccurrences(script, "dotnet build Friggy.sln"));
        Assert.Equal(6, CountOccurrences(script, "--configuration Release"));
        AssertInOrder(
            script,
            "tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj",
            "tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj",
            "tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj",
            "tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj",
            "tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj");
        Assert.Equal(5, CountOccurrences(script, "dotnet test --project"));
        Assert.DoesNotContain(" -- ", script, StringComparison.Ordinal);
    }

    [Fact]
    public void QualityGateScript_RunsRestoreBuildFormatTestsAndAuditInOrder()
    {
        var script = ReadScript("quality-gate.ps1");

        AssertInOrder(
            script,
            "dotnet restore Friggy.sln",
            "pnpm install --frozen-lockfile",
            "dotnet build Friggy.sln",
            "dotnet format Friggy.sln",
            "./scripts/test.ps1 -SkipBuild",
            "pnpm audit --audit-level high",
            "dotnet list Friggy.sln package");
        Assert.Contains("--format json", script, StringComparison.Ordinal);
        Assert.Contains("Get-VulnerabilityCount", script, StringComparison.Ordinal);
        Assert.DoesNotContain(" -- ", script, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualBaselineUpdateScript_RequiresExplicitAcceptanceAndCopiesReviewedActuals()
    {
        var script = ReadScript("update-visual-baselines.ps1");

        Assert.Contains("[switch]$Accept", script, StringComparison.Ordinal);
        Assert.Contains("if (-not $Accept)", script, StringComparison.Ordinal);
        Assert.Contains("TestResults", script, StringComparison.Ordinal);
        Assert.Contains("VisualBaselines", script, StringComparison.Ordinal);
        Assert.Contains("Copy-Item", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove-Item", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadScript(string scriptName) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, "scripts", scriptName));

    private static void AssertInOrder(string content, params string[] expectedFragments)
    {
        var previousIndex = -1;
        foreach (var expectedFragment in expectedFragments)
        {
            var currentIndex = content.IndexOf(expectedFragment, StringComparison.Ordinal);
            Assert.True(
                currentIndex > previousIndex,
                $"No se encontró '{expectedFragment}' después de la posición {previousIndex}.");
            previousIndex = currentIndex;
        }
    }

    private static int CountOccurrences(string content, string value)
    {
        var count = 0;
        var currentIndex = 0;
        while ((currentIndex = content.IndexOf(value, currentIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            currentIndex += value.Length;
        }

        return count;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Friggy.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException(
            "No se encontró la raíz del repositorio desde el directorio de ejecución de los tests.");
    }
}
