using System.Globalization;

namespace Friggy.Web;

/// <summary>Formatting policy for quantities displayed by the Web application.</summary>
internal static class QuantityFormatter
{
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-ES");

    public static string Format(decimal value) => value.ToString("0.###", SpanishCulture);
}
