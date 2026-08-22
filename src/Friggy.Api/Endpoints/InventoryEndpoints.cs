using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>
/// Define los endpoints HTTP para consultar y operar sobre los lotes de inventario.
/// </summary>
public static class InventoryEndpoints
{
    /// <summary>
    /// Registra las rutas de consulta, creación y operaciones sobre lotes de inventario.
    /// </summary>
    /// <param name="routes">Constructor de rutas donde se registra el grupo de endpoints.</param>
    /// <returns>Grupo de rutas configurado para el inventario.</returns>
    public static RouteGroupBuilder MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/inventory-lots").WithTags("Inventory");

        group.MapGet("/", ListAsync)
            .WithName("ListInventoryLots")
            .Produces<IReadOnlyList<InventoryLotResponse>>();
        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetInventoryLot")
            .Produces<InventoryLotResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync)
            .WithName("CreateInventoryLot")
            .Produces<InventoryLotResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}/expiration", CorrectExpirationAsync)
            .WithName("CorrectInventoryLotExpiration")
            .Produces<InventoryLotResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/consume", ConsumeAsync)
            .WithName("ConsumeInventoryLot")
            .Produces<InventoryOperationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/discard", DiscardAsync)
            .WithName("DiscardInventoryLot")
            .Produces<InventoryOperationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/adjust", AdjustAsync)
            .WithName("AdjustInventoryLot")
            .Produces<InventoryLotResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<Ok<IReadOnlyList<InventoryLotResponse>>> ListAsync(
        bool includeUnavailable,
        InventoryLotService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(includeUnavailable, cancellationToken));

    private static async Task<Ok<InventoryLotResponse>> GetAsync(
        Guid id,
        InventoryLotService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken));

    private static async Task<Created<InventoryLotResponse>> CreateAsync(
        CreateInventoryLotRequest request,
        InventoryLotService service,
        CancellationToken cancellationToken)
    {
        var lot = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/inventory-lots/{lot.Id}", lot);
    }

    private static async Task<Ok<InventoryLotResponse>> CorrectExpirationAsync(
        Guid id,
        CorrectInventoryExpirationRequest request,
        InventoryLotService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.CorrectExpirationAsync(id, request, cancellationToken));

    private static async Task<Ok<InventoryOperationResponse>> ConsumeAsync(
        Guid id,
        InventoryQuantityRequest request,
        InventoryLotService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ConsumeAsync(id, request, cancellationToken));

    private static async Task<Ok<InventoryOperationResponse>> DiscardAsync(
        Guid id,
        InventoryQuantityRequest request,
        InventoryLotService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.DiscardAsync(id, request, cancellationToken));

    private static async Task<Ok<InventoryLotResponse>> AdjustAsync(
        Guid id,
        AdjustInventoryLotRequest request,
        InventoryLotService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.AdjustAsync(id, request, cancellationToken));

}
