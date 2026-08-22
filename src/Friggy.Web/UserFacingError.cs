using System.Net;
using Friggy.Web.Api;

namespace Friggy.Web;

internal sealed record UserFacingError(string Title, string Message, bool CanRetry)
{
    public static UserFacingError From(Exception exception, string action) => exception switch
    {
        ApiProblemException { StatusCode: HttpStatusCode.Conflict } => new(
            "No se pudo completar la operación",
            "El dato cambió mientras lo estabas usando. Actualiza la página y vuelve a intentarlo.",
            true),
        ApiProblemException { StatusCode: HttpStatusCode.BadRequest } => new(
            "Revisa los datos introducidos",
            "Hay algún valor que no es válido. Corrígelo e inténtalo de nuevo.",
            false),
        HttpRequestException => new(
            "No se pudo conectar",
            $"No pudimos {action}. Comprueba tu conexión y vuelve a intentarlo.",
            true),
        _ => new(
            "No se pudo completar la operación",
            $"No pudimos {action}. Inténtalo de nuevo en unos instantes.",
            true),
    };
}
