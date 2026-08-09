using Friggy.Application.Recipes.Dtos;
using Friggy.Application.Recipes.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

public static class RecipeEndpoints
{
    public static RouteGroupBuilder MapRecipeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/recipes").WithTags("Recipes");

        group.MapGet("/", ListAsync)
            .WithName("ListRecipes")
            .Produces<IReadOnlyList<RecipeListItemResponse>>();

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetRecipe")
            .Produces<RecipeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateRecipe")
            .Produces<RecipeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateRecipe")
            .Produces<RecipeResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteRecipe")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
