using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.MealTypes.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>
/// Define los endpoints HTTP para gestionar los tipos de comida del catálogo.
/// </summary>
public static class MealTypeEndpoints
{
    /// <summary>
    /// Registra las rutas de consulta, creación, actualización y eliminación de tipos de comida.
    /// </summary>
    /// <param name="routes">Constructor de rutas donde se registra el grupo de endpoints.</param>
    /// <returns>Grupo de rutas configurado para los tipos de comida.</returns>
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
