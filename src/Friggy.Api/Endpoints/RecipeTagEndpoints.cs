using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>
/// Define los endpoints HTTP para gestionar las etiquetas de recetas del catálogo.
/// </summary>
public static class RecipeTagEndpoints
{
    /// <summary>
    /// Registra las rutas de consulta, creación, actualización y eliminación de etiquetas de recetas.
    /// </summary>
    /// <param name="routes">Constructor de rutas donde se registra el grupo de endpoints.</param>
    /// <returns>Grupo de rutas configurado para las etiquetas de recetas.</returns>
    public static RouteGroupBuilder MapRecipeTagEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/recipe-tags").WithTags("Recipe tags");
        group.MapGet("/", async (RecipeTagService service, CancellationToken token) => TypedResults.Ok(await service.ListAsync(token)));
        group.MapGet("/{id:guid}", async (Guid id, RecipeTagService service, CancellationToken token) => TypedResults.Ok(await service.GetAsync(id, token)));
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", async (Guid id, UpdateRecipeTagRequest request, RecipeTagService service, CancellationToken token) => TypedResults.Ok(await service.UpdateAsync(id, request, token)));
        group.MapDelete("/{id:guid}", DeleteAsync);
        return group;
    }

    private static async Task<Created<RecipeTagResponse>> CreateAsync(CreateRecipeTagRequest request, RecipeTagService service, CancellationToken token)
    {
        var result = await service.CreateAsync(request, token);
        return TypedResults.Created($"/api/recipe-tags/{result.Id}", result);
    }

    private static async Task<NoContent> DeleteAsync(Guid id, RecipeTagService service, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return TypedResults.NoContent();
    }
}
