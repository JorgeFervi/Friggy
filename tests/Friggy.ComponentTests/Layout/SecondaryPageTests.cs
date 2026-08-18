using Bunit;
using Friggy.ComponentTests.Testing;

namespace Friggy.ComponentTests.Layout;

public sealed class SecondaryPageTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void NotFound_RendersSpanishMessageAndHomeAction()
    {
        var component = Render<global::Friggy.Web.Components.Pages.NotFound>();

        Assert.Equal("Página no encontrada", component.Find("h1").TextContent);
        Assert.Equal("/", component.Find("a").GetAttribute("href"));
        Assert.Contains("Volver al inicio", component.Find("a").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Error_RendersSafeSpanishMessageAndHomeAction()
    {
        var component = Render<global::Friggy.Web.Components.Pages.Error>();

        Assert.Equal("Algo no ha salido bien", component.Find("h1").TextContent);
        Assert.Equal("/", component.Find("a").GetAttribute("href"));
        Assert.DoesNotContain("Development Mode", component.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_ENVIRONMENT", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void ReconnectModal_UsesAccessibleSpanishDialogAndActions()
    {
        var component = Render<global::Friggy.Web.Components.Layout.ReconnectModal>();
        var dialog = component.Find("dialog");

        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.NotNull(dialog.GetAttribute("aria-labelledby"));
        Assert.Contains("Conexión interrumpida", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Reintentar", component.Find("#components-reconnect-button").TextContent, StringComparison.Ordinal);
        Assert.Contains("Reanudar", component.Find("#components-resume-button").TextContent, StringComparison.Ordinal);
    }
}
