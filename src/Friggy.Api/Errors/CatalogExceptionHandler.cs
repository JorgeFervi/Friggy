using Friggy.Application.Catalogs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Api.Errors;

public sealed class CatalogExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var failure = CatalogFailureClassifier.Classify(exception);
        var (status, title, code) = failure?.Kind switch
        {
            CatalogFailureKind.Validation =>
                (StatusCodes.Status400BadRequest, "La solicitud no es válida.", failure!.Code),
            CatalogFailureKind.NotFound =>
                (StatusCodes.Status404NotFound, "No se encontró el recurso.", failure!.Code),
            CatalogFailureKind.Conflict =>
                (StatusCodes.Status409Conflict, "La operación produce un conflicto.", failure!.Code),
            _ when exception is DbUpdateException =>
                (StatusCodes.Status409Conflict, "La operación produce un conflicto.", "catalog.persistence.conflict"),
            _ => default,
        };

        if (status == 0)
        {
            return false;
        }

        await Results.Problem(
                statusCode: status,
                title: title,
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = code })
            .ExecuteAsync(httpContext);

        return true;
    }
}
