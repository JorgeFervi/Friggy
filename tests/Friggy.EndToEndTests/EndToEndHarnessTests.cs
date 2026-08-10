using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Friggy.EndToEndTests;

public sealed class EndToEndHarnessTests
{
    [Fact]
    [Trait("Category", "E2E")]
    public void Load_MissingWebBaseUrl_FailsWithActionableMessage()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EndToEndSettings.Load(_ => null));

        Assert.Contains("FRIGGY_WEB_BASE_URL", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public void Load_AbsoluteHttpUrl_ConfiguresDeterministicHarness()
    {
        var settings = EndToEndSettings.Load(
            name => name == "FRIGGY_WEB_BASE_URL" ? "http://127.0.0.1:5180/" : null);

        Assert.Equal(new Uri("http://127.0.0.1:5180/"), settings.WebBaseUrl);
        Assert.Equal(1280, settings.Viewport.Width);
        Assert.Equal(720, settings.Viewport.Height);
        Assert.Equal("es-ES", settings.Locale);
        Assert.Equal("Europe/Madrid", settings.TimezoneId);
        Assert.Equal(ColorScheme.Light, settings.ColorScheme);
        Assert.Equal(ReducedMotion.Reduce, settings.ReducedMotion);
        Assert.Equal(1f, settings.DeviceScaleFactor);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public void FriggyPageTest_UsesOfficialPlaywrightXunitHarness()
    {
        Assert.True(typeof(FriggyPageTest).IsAbstract);
        Assert.True(typeof(FriggyPageTest).IsSubclassOf(typeof(PageTest)));
    }
}
