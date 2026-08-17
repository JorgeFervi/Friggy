using System.Diagnostics;
using System.Text.Json;

namespace Friggy.IntegrationTests.Tooling;

public sealed class VisualFoundationContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    [Trait("Category", "Tooling")]
    public void AppDocument_Always_UsesSpanishAndOnlyLocalVisualAssets()
    {
        var appDocument = ReadRepositoryFile("src", "Friggy.Web", "Components", "App.razor");
        var appStyles = ReadRepositoryFile("src", "Friggy.Web", "wwwroot", "app.css");

        Assert.Contains("<html lang=\"es\">", appDocument, StringComparison.Ordinal);
        Assert.Contains("css/fonts.css", appStyles, StringComparison.Ordinal);
        Assert.Contains("css/tokens.css", appStyles, StringComparison.Ordinal);
        Assert.Contains("css/base.css", appStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", appDocument, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", appDocument, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("http://", appStyles, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", appStyles, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public void FontStyles_Always_ReferenceVersionedLocalWoff2FilesAndLicenses()
    {
        var fontStyles = ReadRepositoryFile("src", "Friggy.Web", "wwwroot", "css", "fonts.css");

        Assert.Equal(5, CountOccurrences(fontStyles, "@font-face"));
        Assert.Equal(5, CountOccurrences(fontStyles, "format(\"woff2\")"));
        Assert.DoesNotContain("http://", fontStyles, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", fontStyles, StringComparison.OrdinalIgnoreCase);

        AssertFontAssetExists("Inter-Regular.woff2");
        AssertFontAssetExists("Inter-Medium.woff2");
        AssertFontAssetExists("Inter-SemiBold.woff2");
        AssertFontAssetExists("Inter-Bold.woff2");
        AssertFontAssetExists("Fraunces-SemiBold.woff2");
        AssertRepositoryFileExists("src", "Friggy.Web", "wwwroot", "fonts", "Inter-LICENSE.txt");
        AssertRepositoryFileExists("src", "Friggy.Web", "wwwroot", "fonts", "Fraunces-LICENSE.txt");
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public void DesignTokens_Always_ExposeApprovedPaletteAndResponsiveFoundation()
    {
        var tokens = ReadRepositoryFile("src", "Friggy.Web", "wwwroot", "css", "tokens.css");
        var baseStyles = ReadRepositoryFile("src", "Friggy.Web", "wwwroot", "css", "base.css");

        Assert.Contains("--color-primary-700: #284e63", tokens, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--color-primary-600: #356b85", tokens, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--color-canvas: #f3f6f7", tokens, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--color-accent: #b7794c", tokens, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--font-family-body", tokens, StringComparison.Ordinal);
        Assert.Contains("--font-family-display", tokens, StringComparison.Ordinal);
        Assert.Contains("--breakpoint-tablet: 48rem", tokens, StringComparison.Ordinal);
        Assert.Contains("--breakpoint-desktop: 75rem", tokens, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion: reduce", baseStyles, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", baseStyles, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public async Task VisualComparator_Always_PassesItsDeterministicContractSuite()
    {
        var nodePath = Environment.GetEnvironmentVariable("FRIGGY_NODE_PATH") ?? "node";
        var testScript = Path.Combine(RepositoryRoot, "scripts", "test-visual-comparator.mjs");
        var startInfo = new ProcessStartInfo(nodePath)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--test");
        startInfo.ArgumentList.Add(testScript);

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("No se pudo iniciar Node para probar el comparador visual.");
        var outputTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"El contrato del comparador visual falló.{Environment.NewLine}{output}{Environment.NewLine}{error}");
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public void VisualTooling_Always_UsesLockedTestOnlyDependencies()
    {
        var packageJson = JsonDocument.Parse(ReadRepositoryFile("package.json"));
        var root = packageJson.RootElement;
        var dependencies = root.GetProperty("devDependencies");

        Assert.True(root.GetProperty("private").GetBoolean());
        Assert.True(dependencies.TryGetProperty("pixelmatch", out _));
        Assert.True(dependencies.TryGetProperty("pngjs", out _));
        Assert.Equal(2, dependencies.EnumerateObject().Count());
        AssertRepositoryFileExists("pnpm-lock.yaml");
    }

    private static string ReadRepositoryFile(params string[] relativePath) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. relativePath]));

    private static void AssertRepositoryFileExists(params string[] relativePath) =>
        Assert.True(
            File.Exists(Path.Combine([RepositoryRoot, .. relativePath])),
            $"No existe el archivo requerido '{Path.Combine(relativePath)}'.");

    private static void AssertFontAssetExists(string fileName)
    {
        var path = Path.Combine(
            RepositoryRoot,
            "src",
            "Friggy.Web",
            "wwwroot",
            "fonts",
            fileName);
        var file = new FileInfo(path);

        Assert.True(file.Exists, $"No existe la fuente local '{fileName}'.");
        Assert.True(file.Length > 1_000, $"La fuente local '{fileName}' está vacía o truncada.");
    }

    private static int CountOccurrences(string content, string value) =>
        content.Split(value, StringSplitOptions.None).Length - 1;

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Friggy.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"No se encontró Friggy.sln desde '{AppContext.BaseDirectory}'.");
    }
}
