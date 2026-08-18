using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests.Testing;

internal static class VisualSnapshot
{
    private const string NodePathVariable = "FRIGGY_NODE_PATH";
    private const string StabilityStyles = """
        *,
        *::before,
        *::after {
            animation-delay: 0s !important;
            animation-duration: 0s !important;
            caret-color: transparent !important;
            transition-delay: 0s !important;
            transition-duration: 0s !important;
        }

        [data-visual-hide] {
            visibility: hidden !important;
        }
        """;
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    public static async Task AssertAsync(
        IPage page,
        string snapshotName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        ValidateSnapshotName(snapshotName);

        var platformName = GetPlatformName();
        var baselinePath = Path.Combine(
            RepositoryRoot,
            "tests",
            "Friggy.EndToEndTests",
            "VisualBaselines",
            platformName,
            $"{snapshotName}.png");
        var resultDirectory = Path.Combine(
            RepositoryRoot,
            "TestResults",
            "visual",
            platformName);
        var actualDirectory = Path.Combine(resultDirectory, "actual");
        var diffDirectory = Path.Combine(resultDirectory, "diff");
        var actualPath = Path.Combine(actualDirectory, $"{snapshotName}.png");
        var diffPath = Path.Combine(diffDirectory, $"{snapshotName}.png");

        Directory.CreateDirectory(actualDirectory);
        Directory.CreateDirectory(diffDirectory);

        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = StabilityStyles });
        await page.EvaluateAsync("() => document.fonts.ready");
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Animations = ScreenshotAnimations.Disabled,
            FullPage = true,
            Path = actualPath,
            Scale = ScreenshotScale.Css,
        });

        await CompareAsync(baselinePath, actualPath, diffPath, cancellationToken);
    }

    private static async Task CompareAsync(
        string baselinePath,
        string actualPath,
        string diffPath,
        CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(RepositoryRoot, "scripts", "compare-visual.mjs");
        var startInfo = new ProcessStartInfo(GetNodeExecutable())
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = RepositoryRoot,
        };
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(baselinePath);
        startInfo.ArgumentList.Add(actualPath);
        startInfo.ArgumentList.Add(diffPath);

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("No se pudo iniciar el comparador visual.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode == 0)
        {
            return;
        }

        var message = new StringBuilder()
            .AppendLine("La regresión visual no coincide con el baseline aprobado.")
            .AppendLine(error.Trim())
            .AppendLine(output.Trim())
            .Append("Actual: ")
            .AppendLine(actualPath)
            .Append("Diff: ")
            .AppendLine(diffPath)
            .ToString();
        throw new InvalidOperationException(message);
    }

    private static string GetNodeExecutable() =>
        Environment.GetEnvironmentVariable(NodePathVariable) ?? "node";

    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows-chromium";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux-chromium";
        }

        return "macos-chromium";
    }

    private static void ValidateSnapshotName(string snapshotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotName);
        if (snapshotName.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException(
                "El nombre del snapshot solo puede contener letras ASCII, números y guiones.",
                nameof(snapshotName));
        }
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
