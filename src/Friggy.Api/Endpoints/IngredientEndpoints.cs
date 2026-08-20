using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.Ingredients.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>
/// Define los endpoints HTTP para gestionar los ingredientes del catálogo.
/// </summary>
public static class IngredientEndpoints
{
    /// <summary>
    /// Registra las rutas de consulta, creación, actualización y eliminación de ingredientes.
    /// </summary>
    /// <param name="routes">Constructor de rutas donde se registra el grupo de endpoints.</param>
    /// <returns>Grupo de rutas configurado para los ingredientes.</returns>
    public static RouteGroupBuilder MapIngredientEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ingredients").WithTags("Ingredients");
        group.MapGet("/", ListAsync).WithName("ListIngredients");
        group.MapGet("/{id:guid}", GetAsync).WithName("GetIngredient");
        group.MapPost("/", CreateAsync).WithName("CreateIngredient");
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateIngredient");
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteIngredient");
        return group;
    }

    private static async Task<Ok<IReadOnlyList<IngredientResponse>>> ListAsync(IngredientService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(cancellationToken));

    private static async Task<Ok<IngredientResponse>> GetAsync(Guid id, IngredientService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken));

    private static async Task<Created<IngredientResponse>> CreateAsync(CreateIngredientRequest request, IngredientService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/ingredients/{result.Id}", result);
    }

    private static async Task<Ok<IngredientResponse>> UpdateAsync(Guid id, UpdateIngredientRequest request, IngredientService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<NoContent> DeleteAsync(Guid id, IngredientService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
