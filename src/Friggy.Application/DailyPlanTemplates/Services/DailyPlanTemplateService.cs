using Friggy.Application.DailyPlanTemplates.Dtos;
using Friggy.Application.DailyPlanTemplates.Exceptions;
using Friggy.Application.DailyPlanTemplates.Interfaces;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.DailyPlans.Exceptions;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlanTemplates;
using Friggy.Domain.DailyPlans;

namespace Friggy.Application.DailyPlanTemplates.Services;

/// <summary>Coordina la creación y materialización conservadora de plantillas.</summary>
public sealed class DailyPlanTemplateService(
    IDailyPlanTemplateRepository templates,
    IDailyPlanRepository dailyPlans,
    IDailyPlanReferenceRepository references)
{
    public async Task<IReadOnlyList<DailyPlanTemplateResponse>> ListAsync(
        CancellationToken cancellationToken) =>
        await MapTemplatesAsync(await templates.ListAsync(cancellationToken), cancellationToken);

    public async Task<DailyPlanTemplateResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        (await MapTemplatesAsync([await FindAsync(id, cancellationToken)], cancellationToken))[0];

    public async Task<DailyPlanTemplateResponse> CreateAsync(
        CreateDailyPlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var planTemplate = DailyPlanTemplate.Create(request.Name);
        planTemplate.ReplaceMeals(ToDefinitions(request.Meals));
        await EnsureReferencesExistAsync(planTemplate, cancellationToken);
        await templates.AddAsync(planTemplate, cancellationToken);
        await templates.SaveChangesAsync(cancellationToken);
        return await GetAsync(planTemplate.Id, cancellationToken);
    }

    public async Task<DailyPlanTemplateResponse> UpdateAsync(
        Guid id,
        UpdateDailyPlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var planTemplate = await FindAsync(id, cancellationToken);
        planTemplate.Rename(request.Name);
        planTemplate.ReplaceMeals(ToDefinitions(request.Meals));
        await EnsureReferencesExistAsync(planTemplate, cancellationToken);
        await templates.SaveChangesAsync(cancellationToken);
        return await GetAsync(planTemplate.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        templates.Remove(await FindAsync(id, cancellationToken));
        await templates.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApplyDailyPlanTemplateResponse> ApplyAsync(
        Guid templateId,
        ApplyDailyPlanTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dates = request.Dates?.ToArray() ?? throw InvalidDates();
        if (dates.Length == 0 || dates.Distinct().Count() != dates.Length)
        {
            throw InvalidDates();
        }

        var template = await FindAsync(templateId, cancellationToken);
        var conflicts = await dailyPlans.ListExistingDatesAsync(dates, cancellationToken);
        if (conflicts.Count > 0)
        {
            throw new DailyPlanTemplateConflictException(
                "daily-plan-template.dates.conflict",
                "Ya existe un plan en una o varias fechas seleccionadas.",
                conflicts.OrderBy(date => date).ToArray());
        }

        await EnsureReferencesExistAsync(template, cancellationToken);
        var created = dates.OrderBy(date => date).Select(template.Instantiate).ToArray();
        await dailyPlans.AddRangeAsync(created, cancellationToken);
        try
        {
            await dailyPlans.SaveChangesAsync(cancellationToken);
        }
        catch (DailyPlanDateConflictException)
        {
            throw new DailyPlanTemplateConflictException(
                "daily-plan-template.dates.conflict",
                "Otra operación creó un plan en una fecha seleccionada.",
                dates.OrderBy(date => date).ToArray());
        }
        return new ApplyDailyPlanTemplateResponse(
            template.Id,
            await MapPlansAsync(created, cancellationToken));
    }

    private async Task<DailyPlanTemplate> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await templates.GetByIdAsync(id, cancellationToken) ??
        throw new DailyPlanTemplateNotFoundException(
            "daily-plan-template.not-found",
            "No se encontró la plantilla de plan diario.");

    private async Task<DailyPlanTemplateResponse[]> MapTemplatesAsync(
        IReadOnlyCollection<DailyPlanTemplate> source,
        CancellationToken cancellationToken)
    {
        var mealTypes = (await references.ListMealTypesAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        return source.Select(planTemplate => new DailyPlanTemplateResponse(
            planTemplate.Id,
            planTemplate.Name.Value,
            planTemplate.Meals.OrderBy(meal => meal.Order).Select(meal =>
                new DailyPlanTemplateMealResponse(
                    meal.Id,
                    meal.MealTypeId,
                    mealTypes[meal.MealTypeId].Name.Value,
                    meal.RecipeId,
                    meal.Servings,
                    meal.PlannedTime?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    meal.Order)).ToArray())).ToArray();
    }

    private async Task EnsureReferencesExistAsync(
        DailyPlanTemplate template,
        CancellationToken cancellationToken)
    {
        foreach (var meal in template.Meals)
        {
            if (!await references.MealTypeExistsAsync(meal.MealTypeId, cancellationToken))
            {
                throw new DomainValidationException(
                    "daily-plan-template.meal-type.not-found",
                    "No se encontró el tipo de comida de la plantilla.");
            }

            if (meal.RecipeId.HasValue && !await references.RecipeExistsAsync(meal.RecipeId.Value, cancellationToken))
            {
                throw new DomainValidationException(
                    "daily-plan-template.recipe.not-found",
                    "No se encontró la receta de la plantilla.");
            }
        }
    }

    private async Task<DailyPlanResponse[]> MapPlansAsync(
        IReadOnlyCollection<DailyPlan> plans,
        CancellationToken cancellationToken)
    {
        var mealTypes = (await references.ListMealTypesAsync(cancellationToken))
            .ToDictionary(item => item.Id);
        return plans.Select(plan => new DailyPlanResponse(
            plan.Id,
            plan.Date,
            plan.Slots.OrderBy(slot => slot.Order).Select(slot =>
            {
                var entry = plan.Entries.SingleOrDefault(item => item.MealTypeId == slot.MealTypeId);
                var mealType = mealTypes[slot.MealTypeId];
                return new DailyPlanMealResponse(
                    mealType.Id,
                    mealType.Name.Value,
                    mealType.Order,
                    entry?.RecipeId,
                    entry?.Servings ?? 1,
                    false,
                    null,
                    slot.Id,
                    slot.Order,
                    slot.PlannedTime?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    null,
                    MealPlanEntryState.Planned,
                    null,
                    null);
            }).ToArray())).ToArray();
    }

    private static DomainValidationException InvalidDates() => new(
        "daily-plan-template.dates.invalid",
        "Indica al menos una fecha y no la repitas.");

    private static DailyPlanTemplateMealDefinition[] ToDefinitions(
        IReadOnlyList<DailyPlanTemplateMealRequest>? meals) =>
        meals?.Select(meal => new DailyPlanTemplateMealDefinition(
            meal.MealTypeId,
            meal.RecipeId,
            meal.Servings,
            ParsePlannedTime(meal.PlannedTime),
            meal.Order)).ToArray() ??
        throw new DomainValidationException(
            "daily-plan-template.meals.required",
            "Las comidas de la plantilla son obligatorias.");

    private static TimeOnly? ParsePlannedTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeOnly.TryParseExact(
            value,
            "HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var time))
        {
            return time;
        }

        throw new DomainValidationException(
            "daily-plan-template.meal.planned-time.invalid",
            "La hora prevista debe usar el formato HH:mm.");
    }
}
