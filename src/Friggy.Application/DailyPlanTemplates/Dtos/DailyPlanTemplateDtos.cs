using Friggy.Application.DailyPlans.Dtos;

namespace Friggy.Application.DailyPlanTemplates.Dtos;

public sealed record DailyPlanTemplateMealRequest(
    Guid MealTypeId,
    Guid? RecipeId,
    int Servings,
    string? PlannedTime,
    int Order);

public sealed record CreateDailyPlanTemplateRequest(
    string? Name,
    IReadOnlyList<DailyPlanTemplateMealRequest>? Meals);

public sealed record UpdateDailyPlanTemplateRequest(
    string? Name,
    IReadOnlyList<DailyPlanTemplateMealRequest>? Meals);

public sealed record ApplyDailyPlanTemplateRequest(IReadOnlyList<DateOnly>? Dates);

public sealed record DailyPlanTemplateMealResponse(
    Guid Id,
    Guid MealTypeId,
    string MealTypeName,
    Guid? RecipeId,
    int Servings,
    string? PlannedTime,
    int Order);

public sealed record DailyPlanTemplateResponse(
    Guid Id,
    string Name,
    IReadOnlyList<DailyPlanTemplateMealResponse> Meals);

public sealed record ApplyDailyPlanTemplateResponse(
    Guid TemplateId,
    IReadOnlyList<DailyPlanResponse> CreatedPlans);
