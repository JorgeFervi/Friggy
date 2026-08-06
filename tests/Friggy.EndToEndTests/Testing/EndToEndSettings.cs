using Microsoft.Playwright;

namespace Friggy.EndToEndTests.Testing;

public sealed record EndToEndSettings(
    Uri WebBaseUrl,
    ViewportSize Viewport,
    string Locale,
    string TimezoneId)
{
    public static EndToEndSettings Load(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var baseUrlValue = getEnvironmentVariable("FRIGGY_WEB_BASE_URL");
        if (!Uri.TryCreate(baseUrlValue, UriKind.Absolute, out var baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "FRIGGY_WEB_BASE_URL debe contener una URL HTTP o HTTPS absoluta antes de ejecutar E2E.");
        }

        return new EndToEndSettings(
            baseUrl,
            new ViewportSize { Width = 1280, Height = 720 },
            "es-ES",
            "Europe/Madrid");
    }
}
