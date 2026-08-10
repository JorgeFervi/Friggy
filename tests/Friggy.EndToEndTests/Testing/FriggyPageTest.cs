using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Friggy.EndToEndTests.Testing;

public abstract class FriggyPageTest : PageTest
{
    private readonly List<string> browserLogs = [];
    private readonly EndToEndSettings settings = EndToEndSettings.Load(
        Environment.GetEnvironmentVariable);
    private readonly string videoStagingDirectory = Path.Combine(
        Path.GetTempPath(),
        "friggy-playwright",
        Guid.NewGuid().ToString("N"));

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = settings.WebBaseUrl.AbsoluteUri,
        ViewportSize = settings.Viewport,
        Locale = settings.Locale,
        TimezoneId = settings.TimezoneId,
        ColorScheme = settings.ColorScheme,
        ReducedMotion = settings.ReducedMotion,
        DeviceScaleFactor = settings.DeviceScaleFactor,
        RecordVideoDir = videoStagingDirectory,
        RecordVideoSize = new RecordVideoSize
        {
            Width = settings.Viewport.Width,
            Height = settings.Viewport.Height,
        },
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Page.Console += (_, message) => browserLogs.Add($"{message.Type}: {message.Text}");
        Page.PageError += (_, error) => browserLogs.Add($"page-error: {error}");
        Page.RequestFailed += (_, request) => browserLogs.Add(
            $"request-failed: {request.Method} {request.Url} {request.Failure}");
        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    protected async Task RunScenarioAsync(
        Func<Task> scenario,
        [CallerMemberName] string scenarioName = "scenario")
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ClearArtifactDirectory(scenarioName);

        try
        {
            await scenario();
            await Context.Tracing.StopAsync();
            await DeleteSuccessfulVideoAsync();
        }
        catch (Exception testFailure)
        {
            try
            {
                await PreserveFailureArtifactsAsync(scenarioName);
            }
            catch (Exception artifactFailure)
            {
                throw new AggregateException(
                    "El escenario y la captura de artefactos de Playwright han fallado.",
                    testFailure,
                    artifactFailure);
            }

            throw;
        }
    }

    protected async Task NavigateToInteractivePageAsync(string path)
    {
        await Page.GotoAsync(path);
        await Page.Locator("[data-testid='interactive-ready']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
    }

    protected static string GetArtifactDirectory(string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        var artifactRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "playwright-artifacts"));
        var artifactDirectory = Path.GetFullPath(Path.Combine(
            artifactRoot,
            SanitizeFileName(scenarioName)));
        var rootPrefix = artifactRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!artifactDirectory.StartsWith(rootPrefix, comparison))
        {
            throw new InvalidOperationException(
                "El directorio de artefactos debe permanecer dentro de playwright-artifacts.");
        }

        return artifactDirectory;
    }

    private async Task PreserveFailureArtifactsAsync(string scenarioName)
    {
        var artifactDirectory = GetArtifactDirectory(scenarioName);
        Directory.CreateDirectory(artifactDirectory);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(artifactDirectory, "failure.png"),
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
        });
        await Context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = Path.Combine(artifactDirectory, "trace.zip"),
        });
        await File.WriteAllLinesAsync(
            Path.Combine(artifactDirectory, "browser.log"),
            browserLogs,
            Encoding.UTF8);
        await PreserveVideoAsync(artifactDirectory);
    }

    private async Task DeleteSuccessfulVideoAsync()
    {
        var video = Page.Video;
        if (video is null)
        {
            return;
        }

        await Page.CloseAsync();
        await video.DeleteAsync();
        DeleteVideoStagingDirectory();
    }

    private async Task PreserveVideoAsync(string artifactDirectory)
    {
        var video = Page.Video;
        if (video is null)
        {
            return;
        }

        await Page.CloseAsync();
        await video.SaveAsAsync(Path.Combine(artifactDirectory, "failure.webm"));
        await video.DeleteAsync();
        DeleteVideoStagingDirectory();
    }

    private void DeleteVideoStagingDirectory()
    {
        if (Directory.Exists(videoStagingDirectory))
        {
            Directory.Delete(videoStagingDirectory, recursive: true);
        }
    }

    private static void ClearArtifactDirectory(string scenarioName)
    {
        var artifactDirectory = GetArtifactDirectory(scenarioName);
        if (Directory.Exists(artifactDirectory))
        {
            Directory.Delete(artifactDirectory, recursive: true);
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
    }
}
