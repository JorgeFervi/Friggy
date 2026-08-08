using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.MealTypes.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

public static class MealTypeEndpoints
{
    public static RouteGroupBuilder MapMealTypeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/meal-types").WithTags("Meal types");
        group.MapGet("/", async (MealTypeService service, CancellationToken token) => TypedResults.Ok(await service.ListAsync(token)));
        group.MapGet("/{id:guid}", async (Guid id, MealTypeService service, CancellationToken token) => TypedResults.Ok(await service.GetAsync(id, token)));
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", async (Guid id, UpdateMealTypeRequest request, MealTypeService service, CancellationToken token) => TypedResults.Ok(await service.UpdateAsync(id, request, token)));
        group.MapDelete("/{id:guid}", DeleteAsync);
        return group;
    }

    private static async Task<Created<MealTypeResponse>> CreateAsync(CreateMealTypeRequest request, MealTypeService service, CancellationToken token)
    {
        var result = await service.CreateAsync(request, token);
        return TypedResults.Created($"/api/meal-types/{result.Id}", result);
    }

    private static async Task<NoContent> DeleteAsync(Guid id, MealTypeService service, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return TypedResults.NoContent();
    }
}
