using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Friggy.IntegrationTests.Tooling;

public sealed class VisualFoundationContractTests
{
    private static readonly Regex HexColorPattern = new(
        @"#[0-9a-fA-F]{3,8}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
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
        Assert.Contains("--color-accent: #9f633f", tokens, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--font-family-body", tokens, StringComparison.Ordinal);
        Assert.Contains("--font-family-display", tokens, StringComparison.Ordinal);
        Assert.Contains("--breakpoint-tablet: 48rem", tokens, StringComparison.Ordinal);
        Assert.Contains("--breakpoint-desktop: 75rem", tokens, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion: reduce", baseStyles, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", baseStyles, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public void SelectStyles_SupportedBrowsers_UseCustomizablePickerWithProgressiveFallback()
    {
        var baseStyles = ReadRepositoryFile("src", "Friggy.Web", "wwwroot", "css", "base.css");

        Assert.Contains("@supports (appearance: base-select)", baseStyles, StringComparison.Ordinal);
        Assert.Contains("select::picker(select)", baseStyles, StringComparison.Ordinal);
        Assert.Contains("appearance: base-select", baseStyles, StringComparison.Ordinal);
        Assert.Contains("select::picker-icon", baseStyles, StringComparison.Ordinal);
        Assert.Contains("select:open::picker-icon", baseStyles, StringComparison.Ordinal);
        Assert.Contains("select option::checkmark", baseStyles, StringComparison.Ordinal);
        Assert.Contains("select option:checked", baseStyles, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public void TextInputStyles_AllTextualVariants_PreserveEditingAffordanceAndClearFocus()
    {
        var baseStyles = ReadRepositoryFile("src", "Friggy.Web", "wwwroot", "css", "base.css");

        Assert.Contains("input:is(", baseStyles, StringComparison.Ordinal);
        Assert.Contains("[type=\"text\"]", baseStyles, StringComparison.Ordinal);
        Assert.Contains("[type=\"search\"]", baseStyles, StringComparison.Ordinal);
        Assert.Contains("[type=\"email\"]", baseStyles, StringComparison.Ordinal);
        Assert.Contains("[type=\"url\"]", baseStyles, StringComparison.Ordinal);
        Assert.Contains("[type=\"tel\"]", baseStyles, StringComparison.Ordinal);
        Assert.Contains("[type=\"password\"]", baseStyles, StringComparison.Ordinal);
        Assert.Contains("caret-color: var(--color-primary-700)", baseStyles, StringComparison.Ordinal);
        Assert.Contains("cursor: text", baseStyles, StringComparison.Ordinal);
        Assert.Contains("::placeholder", baseStyles, StringComparison.Ordinal);
        Assert.Contains(":read-only", baseStyles, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", baseStyles, StringComparison.Ordinal);
        Assert.Contains("outline: none", baseStyles, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Tooling")]
    public void RuntimeStyles_Always_KeepExternalResourcesAndHexColorsOutOfComponentStyles()
    {
        var webRoot = Path.Combine(RepositoryRoot, "src", "Friggy.Web");
        var tokenPath = Path.Combine(webRoot, "wwwroot", "css", "tokens.css");
        var stylePaths = Directory.EnumerateFiles(webRoot, "*.css", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(stylePaths);

        foreach (var stylePath in stylePaths)
        {
            var styles = File.ReadAllText(stylePath);
            var relativePath = Path.GetRelativePath(RepositoryRoot, stylePath);

            Assert.DoesNotContain("http://", styles, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("https://", styles, StringComparison.OrdinalIgnoreCase);

            if (!string.Equals(stylePath, tokenPath, StringComparison.OrdinalIgnoreCase))
            {
                Assert.False(
                    HexColorPattern.IsMatch(styles),
                    $"La hoja '{relativePath}' contiene un color hexadecimal fuera de tokens.css.");
            }
        }
    }

    [Theory]
    [InlineData("#284e63", "#ffffff")]
    [InlineData("#356b85", "#ffffff")]
    [InlineData("#9f633f", "#ffffff")]
    [InlineData("#a63f46", "#ffffff")]
    [InlineData("#3e745f", "#ffffff")]
    [InlineData("#5c6b73", "#f3f6f7")]
    [Trait("Category", "Tooling")]
    public void TextPalette_Always_MeetsWcagAaContrast(string foreground, string background)
    {
        var ratio = ContrastRatio(foreground, background);

        Assert.True(
            ratio >= 4.5,
            $"La combinación {foreground} sobre {background} solo alcanza {ratio:F2}:1.");
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

    private static double ContrastRatio(string foreground, string background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);

        return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05) /
            (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
    }

    private static double RelativeLuminance(string color)
    {
        var channels = Enumerable.Range(0, 3)
            .Select(index => Convert.ToInt32(color.Substring(1 + (index * 2), 2), 16) / 255d)
            .Select(channel => channel <= 0.04045
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4))
            .ToArray();

        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
    }

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
