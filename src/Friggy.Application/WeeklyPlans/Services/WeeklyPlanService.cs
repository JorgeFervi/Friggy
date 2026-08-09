using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Application.WeeklyPlans.Exceptions;
using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.WeeklyPlans.Services;

public sealed class WeeklyPlanService(
    IWeeklyPlanRepository plans,
    IWeeklyPlanReferenceRepository references)
{
    public async Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListAsync(
        CancellationToken cancellationToken) =>
        (await plans.ListAsync(cancellationToken))
            .OrderBy(plan => plan.StartDate)
            .ThenBy(plan => plan.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(plan => new WeeklyPlanListItemResponse(
                plan.Id,
                plan.Name.Value,
                plan.StartDate,
                plan.EndDate))
            .ToArray();

    public async Task<WeeklyPlanResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await MapAsync(await FindAsync(id, cancellationToken), cancellationToken);

    public async Task<WeeklyPlanResponse> CreateAsync(
        CreateWeeklyPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = WeeklyPlan.Create(request.Name, request.StartDate, request.Description);
        await EnsureUniqueNameAsync(plan.Name.Normalized, null, cancellationToken);
        await plans.AddAsync(plan, cancellationToken);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    public async Task<WeeklyPlanResponse> UpdateAsync(
        Guid id,
        UpdateWeeklyPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await FindAsync(id, cancellationToken);
        var updatedName = CatalogName.Create(request.Name, "weekly-plan.name.required");
        await EnsureUniqueNameAsync(updatedName.Normalized, plan.Id, cancellationToken);
        plan.UpdateDetails(updatedName.Value, request.Description);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var plan = await FindAsync(id, cancellationToken);
        plans.Remove(plan);
        await plans.SaveChangesAsync(cancellationToken);
    }

    public async Task<WeeklyPlanResponse> SetEntryAsync(
        Guid planId,
        DateOnly date,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await FindAsync(planId, cancellationToken);
        await EnsureReferencesExistAsync(request.RecipeId, mealTypeId, cancellationToken);
        plan.Assign(date, mealTypeId, request.RecipeId);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    public async Task<WeeklyPlanResponse> RemoveEntryAsync(
        Guid planId,
        DateOnly date,
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        var plan = await FindAsync(planId, cancellationToken);
        await EnsureMealTypeExistsAsync(mealTypeId, cancellationToken);
        if (plan.RemoveEntry(date, mealTypeId))
        {
            await plans.SaveChangesAsync(cancellationToken);
        }

        return await MapAsync(plan, cancellationToken);
    }

    private async Task<WeeklyPlan> FindAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await plans.GetByIdAsync(id, cancellationToken) ??
        throw new WeeklyPlanNotFoundException(
            "weekly-plan.not-found",
            "No se encontró el plan semanal.");

    private async Task EnsureUniqueNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (await plans.ExistsByNormalizedNameAsync(
            normalizedName,
            excludingId,
            cancellationToken))
        {
            throw new WeeklyPlanNameConflictException(
                "weekly-plan.name.duplicate",
                "Ya existe un plan semanal con ese nombre.");
        }
    }

    private async Task EnsureReferencesExistAsync(
        Guid recipeId,
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        if (!await references.RecipeExistsAsync(recipeId, cancellationToken))
        {
            throw new WeeklyPlanReferenceNotFoundException(
                "weekly-plan.recipe.not-found",
                "No se encontró la receta.");
        }

        await EnsureMealTypeExistsAsync(mealTypeId, cancellationToken);
    }

    private async Task EnsureMealTypeExistsAsync(
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        if (!await references.MealTypeExistsAsync(mealTypeId, cancellationToken))
        {
            throw new WeeklyPlanReferenceNotFoundException(
                "weekly-plan.meal-type.not-found",
                "No se encontró el tipo de comida.");
        }
    }

    private async Task<WeeklyPlanResponse> MapAsync(
        WeeklyPlan plan,
        CancellationToken cancellationToken)
    {
        var mealTypes = (await references.ListMealTypesAsync(cancellationToken))
            .OrderBy(mealType => mealType.Order)
            .ThenBy(mealType => mealType.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var entries = plan.Entries.ToDictionary(
            entry => (entry.Date, entry.MealTypeId));
        var days = plan.Dates
            .OrderBy(date => date)
            .Select(date => new WeeklyPlanDayResponse(
                date,
                mealTypes
                    .Select(mealType => MapMeal(date, mealType, entries))
                    .ToArray()))
            .ToArray();

        return new WeeklyPlanResponse(
            plan.Id,
            plan.Name.Value,
            plan.StartDate,
            plan.EndDate,
            plan.Description,
            days);
    }

    private static WeeklyPlanMealResponse MapMeal(
        DateOnly date,
        MealType mealType,
        Dictionary<(DateOnly Date, Guid MealTypeId), MealPlanEntry> entries)
    {
        var recipeId = entries.TryGetValue((date, mealType.Id), out var entry)
            ? entry.RecipeId
            : (Guid?)null;

        return new WeeklyPlanMealResponse(
            mealType.Id,
            mealType.Name.Value,
            mealType.Order,
            recipeId);
    }
}
