using Friggy.Application.Recipes.Dtos;
using Friggy.Application.Recipes.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>
/// Define los endpoints HTTP para gestionar las recetas y sus asociaciones con ingredientes.
/// </summary>
public static class RecipeEndpoints
{
    /// <summary>
    /// Registra las rutas de consulta, creación, actualización y eliminación de recetas.
    /// </summary>
    /// <param name="routes">Constructor de rutas donde se registra el grupo de endpoints.</param>
    /// <returns>Grupo de rutas configurado para las recetas.</returns>
    public static RouteGroupBuilder MapRecipeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/recipes").WithTags("Recipes");

        group.MapGet("/", ListAsync)
            .WithName("ListRecipes")
            .Produces<IReadOnlyList<RecipeListItemResponse>>();

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetRecipe")
            .WithSummary("Obtener una receta con sus asociaciones entre pasos e ingredientes")
            .WithDescription(
                "Los identificadores asociados a cada paso siguen el orden de los ingredientes de la receta.")
            .Produces<RecipeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateRecipe")
            .WithSummary("Crear una receta con asociaciones entre pasos e ingredientes")
            .WithDescription(
                "Cada paso puede referenciar por su identificador las líneas de ingrediente incluidas en la solicitud.")
            .Produces<RecipeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateRecipe")
            .WithSummary("Actualizar una receta y sus asociaciones entre pasos e ingredientes")
            .WithDescription(
                "Conserva los identificadores de línea enviados y reemplaza sus asociaciones de forma atómica.")
            .Produces<RecipeResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteRecipe")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<RecipeListItemResponse>>> ListAsync(
        RecipeService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(cancellationToken));

    private static async Task<Ok<RecipeResponse>> GetAsync(
        Guid id,
        RecipeService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken));

    private static async Task<Created<RecipeResponse>> CreateAsync(
        CreateRecipeRequest request,
        RecipeService service,
        CancellationToken cancellationToken)
    {
        var recipe = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/recipes/{recipe.Id}", recipe);
    }

    private static async Task<Ok<RecipeResponse>> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        RecipeService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<NoContent> DeleteAsync(
        Guid id,
        RecipeService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
