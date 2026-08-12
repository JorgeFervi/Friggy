using Friggy.Application.Catalogs;
using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Recipes.Exceptions;
using Friggy.Application.WeeklyPlans.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var failure = Classify(exception);
        if (failure is null)
        {
            return false;
        }

        await Results.Problem(
                statusCode: failure.Status,
                title: failure.Title,
                detail: failure.Detail ?? exception.Message,
                instance: httpContext.Request.Path,
                extensions: new Dictionary<string, object?> { ["code"] = failure.Code })
            .ExecuteAsync(httpContext);

        return true;
    }

    private static ApiFailure? Classify(Exception exception)
    {
        var catalogFailure = CatalogFailureClassifier.Classify(exception);
        if (catalogFailure is not null)
        {
            return catalogFailure.Kind switch
            {
                CatalogFailureKind.Validation => Validation(catalogFailure.Code),
                CatalogFailureKind.NotFound => NotFound(catalogFailure.Code),
                CatalogFailureKind.Conflict => Conflict(catalogFailure.Code),
                _ => null,
            };
        }

        var recipeFailure = RecipeFailureClassifier.Classify(exception);
        if (recipeFailure is not null)
        {
            return recipeFailure.Kind switch
            {
                RecipeFailureKind.NotFound => NotFound(recipeFailure.Code),
                RecipeFailureKind.Conflict => Conflict(recipeFailure.Code),
                _ => null,
            };
        }

        var inventoryFailure = InventoryFailureClassifier.Classify(exception);
        if (inventoryFailure is not null)
        {
            return inventoryFailure.Kind switch
            {
                InventoryFailureKind.NotFound => NotFound(inventoryFailure.Code),
                InventoryFailureKind.Conflict => Conflict(inventoryFailure.Code),
                _ => null,
            };
        }

        var weeklyPlanFailure = WeeklyPlanFailureClassifier.Classify(exception);
        if (weeklyPlanFailure is not null)
        {
            return weeklyPlanFailure.Kind switch
            {
                WeeklyPlanFailureKind.NotFound => NotFound(weeklyPlanFailure.Code),
                WeeklyPlanFailureKind.Conflict => Conflict(weeklyPlanFailure.Code),
                _ => null,
            };
        }

        return exception switch
        {
            DbUpdateException => Conflict(
                "persistence.conflict",
                "La operación entra en conflicto con el estado actual de los datos."),
            _ => null,
        };
    }

    private static ApiFailure Validation(string code) =>
        new(StatusCodes.Status400BadRequest, "La solicitud no es válida.", code);

    private static ApiFailure NotFound(string code) =>
        new(StatusCodes.Status404NotFound, "No se encontró el recurso.", code);

    private static ApiFailure Conflict(string code, string? detail = null) =>
        new(StatusCodes.Status409Conflict, "La operación produce un conflicto.", code, detail);

    private sealed record ApiFailure(int Status, string Title, string Code, string? Detail = null);
}
