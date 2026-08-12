using System.Globalization;
using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Application.WeeklyPlans.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

public static class WeeklyPlanEndpoints
{
    public static RouteGroupBuilder MapWeeklyPlanEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/weekly-plans").WithTags("Weekly plans");

        group.MapGet("/", ListAsync)
            .WithName("ListWeeklyPlans")
            .Produces<IReadOnlyList<WeeklyPlanListItemResponse>>();

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetWeeklyPlan")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateWeeklyPlan")
            .Produces<WeeklyPlanResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateWeeklyPlan")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteWeeklyPlan")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{planId:guid}/days/{date}/meal-types/{mealTypeId:guid}", SetEntryAsync)
            .WithName("SetMealPlanEntry")
            .WithDescription("Asigna o sustituye una receta. La fecha debe usar el formato yyyy-MM-dd.")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{planId:guid}/days/{date}/meal-types/{mealTypeId:guid}", RemoveEntryAsync)
            .WithName("RemoveMealPlanEntry")
            .WithDescription("Retira la receta asignada. La fecha debe usar el formato yyyy-MM-dd.")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{planId:guid}/days/{date}/slots", AddSlotAsync)
            .WithName("AddMealPlanSlot")
            .WithDescription("Añade un hueco al final del día. La fecha debe usar el formato yyyy-MM-dd.")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{planId:guid}/days/{date}/slots/order", ReorderSlotsAsync)
            .WithName("ReorderMealPlanSlots")
            .WithDescription("Reordena todos los huecos del día. La fecha debe usar el formato yyyy-MM-dd.")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{planId:guid}/slots/{slotId:guid}", RemoveSlotAsync)
            .WithName("RemoveMealPlanSlot")
            .Produces<WeeklyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{planId:guid}/slots/{slotId:guid}/time", SetSlotTimeAsync)
            .WithName("SetMealPlanSlotTime")
            .Produces<MealPlanSlotScheduleResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{planId:guid}/days/{date}/meal-types/{mealTypeId:guid}/skip", SkipEntryAsync)
            .WithName("SkipMealPlanEntry")
            .WithDescription("Omite una asignación planificada. La fecha debe usar el formato yyyy-MM-dd.")
            .Produces<MealPlanEntryStateResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<WeeklyPlanListItemResponse>>> ListAsync(
        WeeklyPlanService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(cancellationToken));

    private static async Task<Ok<WeeklyPlanResponse>> GetAsync(
        Guid id,
        WeeklyPlanService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken));

    private static async Task<Created<WeeklyPlanResponse>> CreateAsync(
        CreateWeeklyPlanRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        var plan = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/weekly-plans/{plan.Id}", plan);
    }

    private static async Task<Ok<WeeklyPlanResponse>> UpdateAsync(
        Guid id,
        UpdateWeeklyPlanRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<NoContent> DeleteAsync(
        Guid id,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<WeeklyPlanResponse>, ProblemHttpResult>> SetEntryAsync(
        Guid planId,
        string date,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var parsedDate))
        {
            return InvalidDate(date);
        }

        return TypedResults.Ok(
            await service.SetEntryAsync(
                planId,
                parsedDate,
                mealTypeId,
                request,
                cancellationToken));
    }

    private static async Task<Results<Ok<WeeklyPlanResponse>, ProblemHttpResult>> RemoveEntryAsync(
        Guid planId,
        string date,
        Guid mealTypeId,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var parsedDate))
        {
            return InvalidDate(date);
        }

        return TypedResults.Ok(
            await service.RemoveEntryAsync(
                planId,
                parsedDate,
                mealTypeId,
                cancellationToken));
    }

    private static async Task<Results<Ok<WeeklyPlanResponse>, ProblemHttpResult>> AddSlotAsync(
        Guid planId,
        string date,
        AddMealPlanSlotRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var parsedDate))
        {
            return InvalidDate(date);
        }

        return TypedResults.Ok(
            await service.AddSlotAsync(planId, parsedDate, request, cancellationToken));
    }

    private static async Task<Results<Ok<WeeklyPlanResponse>, ProblemHttpResult>> ReorderSlotsAsync(
        Guid planId,
        string date,
        ReorderMealPlanSlotsRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var parsedDate))
        {
            return InvalidDate(date);
        }

        return TypedResults.Ok(
            await service.ReorderSlotsAsync(planId, parsedDate, request, cancellationToken));
    }

    private static async Task<Ok<WeeklyPlanResponse>> RemoveSlotAsync(
        Guid planId,
        Guid slotId,
        WeeklyPlanService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.RemoveSlotAsync(planId, slotId, cancellationToken));

    private static async Task<Ok<MealPlanSlotScheduleResponse>> SetSlotTimeAsync(
        Guid planId,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(
            await service.SetSlotTimeAsync(planId, slotId, request, cancellationToken));

    private static async Task<Results<Ok<MealPlanEntryStateResponse>, ProblemHttpResult>> SkipEntryAsync(
        Guid planId,
        string date,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var parsedDate))
        {
            return InvalidDate(date);
        }

        return TypedResults.Ok(
            await service.SkipEntryAsync(
                planId,
                parsedDate,
                mealTypeId,
                request,
                cancellationToken));
    }

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static ProblemHttpResult InvalidDate(string value) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "La solicitud no es válida.",
            detail: $"La fecha '{value}' debe usar el formato yyyy-MM-dd.",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "weekly-plan.date.format",
            });
}
