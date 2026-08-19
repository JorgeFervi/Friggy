using System.Globalization;
using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.Application.WeeklyPlans.Exceptions;
using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.WeeklyPlans.Services;

/// <summary>
/// Servicio de aplicación que coordina la gestión de planes semanales,
/// asignaciones y huecos de comida.
/// </summary>
public sealed class WeeklyPlanService(
    IWeeklyPlanRepository plans,
    IWeeklyPlanReferenceRepository references)
{
    /// <summary>
    /// Obtiene los resúmenes de los planes ordenados por fecha y nombre.
    /// </summary>
    public async Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListAsync(
        CancellationToken cancellationToken) =>
        (await plans.ListSummariesAsync(cancellationToken))
            .OrderBy(plan => plan.StartDate)
            .ThenBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    /// <summary>
    /// Obtiene un plan semanal completo por su identificador.
    /// </summary>
    public async Task<WeeklyPlanResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await MapAsync(await FindAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// Crea un plan semanal con sus huecos de comida iniciales.
    /// </summary>
    public async Task<WeeklyPlanResponse> CreateAsync(
        CreateWeeklyPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = WeeklyPlan.Create(request.Name, request.StartDate, request.Description);
        await EnsureUniqueNameAsync(plan.Name.Normalized, null, cancellationToken);
        await AddDefaultSlotsAsync(plan, cancellationToken);
        await plans.AddAsync(plan, cancellationToken);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Actualiza los detalles de un plan semanal.
    /// </summary>
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

    /// <summary>
    /// Elimina un plan semanal.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var plan = await FindAsync(id, cancellationToken);
        plans.Remove(plan);
        await plans.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Asigna o reemplaza una receta en una comida del plan.
    /// </summary>
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
        plan.Assign(date, mealTypeId, request.RecipeId, request.Servings);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Retira una asignación de receta de una comida del plan.
    /// </summary>
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

    /// <summary>
    /// Establece la hora prevista de un hueco de comida.
    /// </summary>
    public async Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
        Guid planId,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plannedTime = ParsePlannedTime(request.PlannedTime);
        var plan = await FindAsync(planId, cancellationToken);
        var slot = plan.Slots.SingleOrDefault(item => item.Id == slotId) ??
            throw new DomainValidationException(
                "weekly-plan.slot.not-found",
                "No se encontró el hueco de comida.");
        var entry = plan.Entries.SingleOrDefault(item =>
            item.Date == slot.Date && item.MealTypeId == slot.MealTypeId);
        var estimatedTime = entry is null
            ? null
            : await references.GetRecipeEstimatedTimeAsync(
                entry.RecipeId,
                cancellationToken);
        plan.SetSlotTime(slotId, plannedTime);
        await plans.SaveChangesAsync(cancellationToken);
        return new MealPlanSlotScheduleResponse(
            slot.Id,
            slot.PlannedTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            slot.GetPreparationStartsAt(estimatedTime));
    }

    /// <summary>
    /// Añade un hueco de comida a un día del plan.
    /// </summary>
    public async Task<WeeklyPlanResponse> AddSlotAsync(
        Guid planId,
        DateOnly date,
        AddMealPlanSlotRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await FindAsync(planId, cancellationToken);
        await EnsureMealTypeExistsAsync(request.MealTypeId, cancellationToken);
        plan.AddSlot(date, request.MealTypeId);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Reordena los huecos de comida de un día.
    /// </summary>
    public async Task<WeeklyPlanResponse> ReorderSlotsAsync(
        Guid planId,
        DateOnly date,
        ReorderMealPlanSlotsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SlotIds);

        var plan = await FindAsync(planId, cancellationToken);
        plan.ReorderSlots(date, request.SlotIds);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Retira un hueco de comida del plan.
    /// </summary>
    public async Task<WeeklyPlanResponse> RemoveSlotAsync(
        Guid planId,
        Guid slotId,
        CancellationToken cancellationToken)
    {
        var plan = await FindAsync(planId, cancellationToken);
        if (plan.RemoveSlot(slotId))
        {
            await plans.SaveChangesAsync(cancellationToken);
        }

        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Omite una comida y registra el motivo y la alternativa indicada.
    /// </summary>
    public async Task<MealPlanEntryStateResponse> SkipEntryAsync(
        Guid planId,
        DateOnly date,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await FindAsync(planId, cancellationToken);
        await EnsureMealTypeExistsAsync(mealTypeId, cancellationToken);
        var entry = plan.SkipEntry(
            date,
            mealTypeId,
            request.Reason,
            request.AlternativeDescription);
        await plans.SaveChangesAsync(cancellationToken);
        return new MealPlanEntryStateResponse(
            entry.Id,
            MapState(entry.Status),
            entry.CompletedAt,
            entry.SkippedReason,
            entry.AlternativeDescription);
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
        var mealTypesById = mealTypes.ToDictionary(mealType => mealType.Id);
        var entries = plan.Entries.ToDictionary(
            entry => (entry.Date, entry.MealTypeId));
        var estimatedTimes = new Dictionary<Guid, TimeSpan?>();
        foreach (var recipeId in plan.Entries.Select(entry => entry.RecipeId).Distinct())
        {
            estimatedTimes[recipeId] = await references.GetRecipeEstimatedTimeAsync(
                recipeId,
                cancellationToken);
        }

        var days = plan.Dates
            .OrderBy(date => date)
            .Select(date => new WeeklyPlanDayResponse(
                date,
                plan.Slots
                    .Where(slot => slot.Date == date)
                    .OrderBy(slot => slot.Order)
                    .Select(slot => MapMeal(
                        slot,
                        mealTypesById[slot.MealTypeId],
                        entries,
                        estimatedTimes))
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

    private async Task AddDefaultSlotsAsync(
        WeeklyPlan plan,
        CancellationToken cancellationToken)
    {
        var mealTypes = (await references.ListMealTypesAsync(cancellationToken))
            .OrderBy(mealType => mealType.Order)
            .ThenBy(mealType => mealType.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        foreach (var date in plan.Dates)
        {
            foreach (var mealType in mealTypes)
            {
                plan.AddSlot(date, mealType.Id);
            }
        }
    }

    private static TimeOnly? ParsePlannedTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var plannedTime))
        {
            return plannedTime;
        }

        throw new DomainValidationException(
            "weekly-plan.slot.planned-time.invalid",
            "La hora prevista debe usar el formato HH:mm.");
    }

    private static WeeklyPlanMealResponse MapMeal(
        MealPlanSlot slot,
        MealType mealType,
        Dictionary<(DateOnly Date, Guid MealTypeId), MealPlanEntry> entries,
        Dictionary<Guid, TimeSpan?> estimatedTimes)
    {
        var hasEntry = entries.TryGetValue((slot.Date, mealType.Id), out var entry);
        var recipeId = hasEntry ? entry!.RecipeId : (Guid?)null;
        var servings = hasEntry ? entry!.Servings : 1;
        var estimatedTime = hasEntry ? estimatedTimes.GetValueOrDefault(entry!.RecipeId) : null;

        return new WeeklyPlanMealResponse(
            mealType.Id,
            mealType.Name.Value,
            mealType.Order,
            recipeId,
            servings,
            entry?.IsCompleted ?? false,
            entry?.CompletedAt,
            slot.Id,
            slot.Order,
            slot.PlannedTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            slot.GetPreparationStartsAt(estimatedTime),
            entry is null ? MealPlanEntryState.Planned : MapState(entry.Status),
            entry?.SkippedReason,
            entry?.AlternativeDescription);
    }

    private static MealPlanEntryState MapState(MealPlanEntryStatus status) => status switch
    {
        MealPlanEntryStatus.Planned => MealPlanEntryState.Planned,
        MealPlanEntryStatus.Completed => MealPlanEntryState.Completed,
        MealPlanEntryStatus.Skipped => MealPlanEntryState.Skipped,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
