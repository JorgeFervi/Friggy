using Friggy.Application.DailyPlanTemplates.Dtos;
using Friggy.Application.DailyPlanTemplates.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>Define las operaciones HTTP de plantillas diarias.</summary>
public static class DailyPlanTemplateEndpoints
{
    public static RouteGroupBuilder MapDailyPlanTemplateEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/daily-plan-templates").WithTags("Daily plan templates");
        group.MapGet("/", ListAsync).WithName("ListDailyPlanTemplates")
            .Produces<IReadOnlyList<DailyPlanTemplateResponse>>();
        group.MapGet("/{id:guid}", GetAsync).WithName("GetDailyPlanTemplate")
            .Produces<DailyPlanTemplateResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync).WithName("CreateDailyPlanTemplate")
            .Produces<DailyPlanTemplateResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateDailyPlanTemplate")
            .Produces<DailyPlanTemplateResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteDailyPlanTemplate")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{id:guid}/apply", ApplyAsync).WithName("ApplyDailyPlanTemplate")
            .Produces<ApplyDailyPlanTemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<Ok<IReadOnlyList<DailyPlanTemplateResponse>>> ListAsync(
        DailyPlanTemplateService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(cancellationToken));

    private static async Task<Ok<DailyPlanTemplateResponse>> GetAsync(
        Guid id, DailyPlanTemplateService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken));

    private static async Task<Created<DailyPlanTemplateResponse>> CreateAsync(
        CreateDailyPlanTemplateRequest request, DailyPlanTemplateService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/daily-plan-templates/{result.Id}", result);
    }

    private static async Task<Ok<DailyPlanTemplateResponse>> UpdateAsync(
        Guid id, UpdateDailyPlanTemplateRequest request, DailyPlanTemplateService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<NoContent> DeleteAsync(
        Guid id, DailyPlanTemplateService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Created<ApplyDailyPlanTemplateResponse>> ApplyAsync(
        Guid id, ApplyDailyPlanTemplateRequest request, DailyPlanTemplateService service, CancellationToken cancellationToken)
    {
        var result = await service.ApplyAsync(id, request, cancellationToken);
        return TypedResults.Created($"/api/daily-plan-templates/{id}/apply", result);
    }
}
