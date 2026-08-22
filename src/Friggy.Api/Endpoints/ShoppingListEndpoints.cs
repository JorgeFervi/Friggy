using System.Globalization;
using Friggy.Application.ShoppingLists.Dtos;
using Friggy.Application.ShoppingLists.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

public static class ShoppingListEndpoints
{
    public static RouteGroupBuilder MapShoppingListEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/shopping-list").WithTags("Shopping list");
        group.MapGet("/", GetAsync).WithName("GetShoppingList")
            .WithDescription("Calcula la comparación inclusiva. Las fechas usan yyyy-MM-dd.")
            .Produces<ShoppingListResponse>().ProducesProblem(StatusCodes.Status400BadRequest);
        return group;
    }

    private static async Task<Results<Ok<ShoppingListResponse>, ProblemHttpResult>> GetAsync(
        string? from, string? to, ShoppingListService service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "La solicitud no es válida.", extensions: new Dictionary<string, object?> { ["code"] = "shopping-list.date.required" });
        }
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "La solicitud no es válida.", extensions: new Dictionary<string, object?> { ["code"] = "shopping-list.date.format" });
        }
        if (startDate > endDate)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "La solicitud no es válida.", extensions: new Dictionary<string, object?> { ["code"] = "shopping-list.date-range.invalid" });
        }
        return TypedResults.Ok(await service.GetAsync(startDate, endDate, cancellationToken));
    }
}
