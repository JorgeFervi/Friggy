using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

public static class UnitTypeEndpoints
{
    public static RouteGroupBuilder MapUnitTypeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/unit-types").WithTags("Unit types");
        group.MapGet("/", async (UnitTypeService service, CancellationToken token) => TypedResults.Ok(await service.ListAsync(token)));
        group.MapGet("/{id:guid}", async (Guid id, UnitTypeService service, CancellationToken token) => TypedResults.Ok(await service.GetAsync(id, token)));
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", async (Guid id, UpdateUnitTypeRequest request, UnitTypeService service, CancellationToken token) => TypedResults.Ok(await service.UpdateAsync(id, request, token)));
        group.MapDelete("/{id:guid}", DeleteAsync);
        return group;
    }

    private static async Task<Created<UnitTypeResponse>> CreateAsync(CreateUnitTypeRequest request, UnitTypeService service, CancellationToken token)
    {
        var result = await service.CreateAsync(request, token);
        return TypedResults.Created($"/api/unit-types/{result.Id}", result);
    }

    private static async Task<NoContent> DeleteAsync(Guid id, UnitTypeService service, CancellationToken token)
    {
        await service.DeleteAsync(id, token);
        return TypedResults.NoContent();
    }
}
