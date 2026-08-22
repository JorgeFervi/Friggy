using System.Globalization;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.DailyPlans.Services;
using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Friggy.Api.Endpoints;

/// <summary>Define la API de planificación diaria.</summary>
public static class DailyPlanEndpoints
{
    /// <summary>Registra las operaciones diarias direccionadas por fecha.</summary>
    public static RouteGroupBuilder MapDailyPlanEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/daily-plans").WithTags("Daily plans");
        group.MapGet("/", ListAsync)
            .WithName("ListDailyPlans")
            .Produces<DailyPlanRangeResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet("/{date}", GetAsync)
            .WithName("GetDailyPlan")
            .Produces<DailyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync)
            .WithName("CreateDailyPlan")
            .Produces<DailyPlanResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{date}", DeleteAsync)
            .WithName("DeleteDailyPlan")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{date}/meal-types/{mealTypeId:guid}", SetEntryAsync)
            .WithName("SetDailyPlanEntry")
            .Produces<DailyPlanResponse>();
        group.MapDelete("/{date}/meal-types/{mealTypeId:guid}", RemoveEntryAsync)
            .WithName("RemoveDailyPlanEntry")
            .Produces<DailyPlanResponse>();
        group.MapPost("/{date}/slots", AddSlotAsync)
            .WithName("AddDailyPlanSlot")
            .Produces<DailyPlanResponse>();
        group.MapPut("/{date}/slots/order", ReorderSlotsAsync)
            .WithName("ReorderDailyPlanSlots")
            .Produces<DailyPlanResponse>();
        group.MapDelete("/{date}/slots/{slotId:guid}", RemoveSlotAsync)
            .WithName("RemoveDailyPlanSlot")
            .Produces<DailyPlanResponse>();
        group.MapPut("/{date}/slots/{slotId:guid}/time", SetSlotTimeAsync)
            .WithName("SetDailyPlanSlotTime")
            .Produces<MealPlanSlotScheduleResponse>();
        group.MapPost("/{date}/meal-types/{mealTypeId:guid}/skip", SkipEntryAsync)
            .WithName("SkipDailyPlanEntry")
            .Produces<MealPlanEntryStateResponse>();
        group.MapGet("/{date}/inventory-requirements", GetRequirementsAsync)
            .WithName("GetDailyPlanInventoryRequirements")
            .Produces<IReadOnlyList<InventoryRequirementResponse>>();
        group.MapPost("/{date}/meal-types/{mealTypeId:guid}/complete", CompleteMealAsync)
            .WithName("CompleteDailyPlanMeal")
            .Produces<MealCompletionResponse>();
        return group;
    }

    private static async Task<Results<Ok<DailyPlanRangeResponse>, ProblemHttpResult>> ListAsync(
        string from,
        string to,
        DailyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(from, out var startDate))
        {
            return InvalidDate(from);
        }

        if (!TryParseDate(to, out var endDate))
        {
            return InvalidDate(to);
        }

        return TypedResults.Ok(await service.ListAsync(startDate, endDate, cancellationToken));
    }

    private static async Task<Results<Ok<DailyPlanResponse>, ProblemHttpResult>> GetAsync(
        string date,
        DailyPlanService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.GetAsync(plannedDate, cancellationToken))
            : InvalidDate(date);

    private static async Task<Created<DailyPlanResponse>> CreateAsync(
        CreateDailyPlanRequest request,
        DailyPlanService service,
        CancellationToken cancellationToken)
    {
        var plan = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/daily-plans/{plan.Date:yyyy-MM-dd}", plan);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        string date,
        DailyPlanService service,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(date, out var plannedDate))
        {
            return InvalidDate(date);
        }

        await service.DeleteAsync(plannedDate, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<DailyPlanResponse>, ProblemHttpResult>> SetEntryAsync(
        string date,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        DailyPlanService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.SetEntryAsync(
                plannedDate, mealTypeId, request, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<DailyPlanResponse>, ProblemHttpResult>> RemoveEntryAsync(
        string date,
        Guid mealTypeId,
        DailyPlanService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.RemoveEntryAsync(
                plannedDate, mealTypeId, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<DailyPlanResponse>, ProblemHttpResult>> AddSlotAsync(
        string date,
        AddMealPlanSlotRequest request,
        DailyPlanService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.AddSlotAsync(plannedDate, request, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<DailyPlanResponse>, ProblemHttpResult>> ReorderSlotsAsync(
        string date,
        ReorderMealPlanSlotsRequest request,
        DailyPlanService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.ReorderSlotsAsync(plannedDate, request, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<DailyPlanResponse>, ProblemHttpResult>> RemoveSlotAsync(
        string date,
        Guid slotId,
        DailyPlanService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.RemoveSlotAsync(plannedDate, slotId, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<MealPlanSlotScheduleResponse>, ProblemHttpResult>>
        SetSlotTimeAsync(
            string date,
            Guid slotId,
            SetMealPlanSlotTimeRequest request,
            DailyPlanService service,
            CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.SetSlotTimeAsync(
                plannedDate, slotId, request, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<MealPlanEntryStateResponse>, ProblemHttpResult>>
        SkipEntryAsync(
            string date,
            Guid mealTypeId,
            SkipMealPlanEntryRequest request,
            DailyPlanService service,
            CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.SkipEntryAsync(
                plannedDate, mealTypeId, request, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<IReadOnlyList<InventoryRequirementResponse>>, ProblemHttpResult>>
        GetRequirementsAsync(
            string date,
            DailyPlanInventoryService service,
            CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.GetRequirementsAsync(plannedDate, cancellationToken))
            : InvalidDate(date);

    private static async Task<Results<Ok<MealCompletionResponse>, ProblemHttpResult>> CompleteMealAsync(
        string date,
        Guid mealTypeId,
        CompleteMealRequest request,
        DailyPlanInventoryService service,
        CancellationToken cancellationToken) =>
        TryParseDate(date, out var plannedDate)
            ? TypedResults.Ok(await service.CompleteMealAsync(
                plannedDate, mealTypeId, request, cancellationToken))
            : InvalidDate(date);

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
                ["code"] = "daily-plan.date.format",
            });
}
