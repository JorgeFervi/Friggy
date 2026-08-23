using System.Globalization;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.DailyPlans.Exceptions;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;

namespace Friggy.Application.DailyPlans.Services;

/// <summary>Coordina los casos de uso de planificación diaria.</summary>
public sealed class DailyPlanService(
    IDailyPlanRepository plans,
    IDailyPlanReferenceRepository references)
{
    /// <summary>Lista los planes existentes del intervalo inclusivo.</summary>
    public async Task<DailyPlanRangeResponse> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw new DomainValidationException(
                "daily-plan.date-range.invalid",
                "La fecha inicial no puede ser posterior a la final.");
        }

        var matchingPlans = await plans.ListBetweenAsync(from, to, cancellationToken);
        return new DailyPlanRangeResponse(
            from,
            to,
            await MapAsync(matchingPlans.OrderBy(plan => plan.Date), cancellationToken));
    }

    /// <summary>Obtiene el plan de una fecha.</summary>
    public async Task<DailyPlanResponse> GetAsync(
        DateOnly date,
        CancellationToken cancellationToken) =>
        await MapAsync(await FindAsync(date, cancellationToken), cancellationToken);

    /// <summary>Crea el plan de una fecha con sus huecos iniciales.</summary>
    public async Task<DailyPlanResponse> CreateAsync(
        CreateDailyPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await plans.GetByDateAsync(request.Date, cancellationToken) is not null)
        {
            throw DuplicateDate(request.Date);
        }

        var plan = DailyPlan.Create(request.Date);
        foreach (var mealType in await OrderedMealTypesAsync(cancellationToken))
        {
            plan.AddSlot(mealType.Id);
        }

        await plans.AddAsync(plan, cancellationToken);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>Elimina el plan de una fecha.</summary>
    public async Task DeleteAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var plan = await FindAsync(date, cancellationToken);
        if (plan.Entries.Any(entry => entry.IsCompleted))
        {
            throw new DailyPlanDateConflictException(
                "daily-plan.completed.delete-conflict",
                "No se puede borrar un plan que contiene comidas completadas.");
        }

        plans.Remove(plan);
        await plans.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Asigna o reemplaza una receta.</summary>
    public async Task<DailyPlanResponse> SetEntryAsync(
        DateOnly date,
        Guid mealTypeId,
        SetMealPlanEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = await FindAsync(date, cancellationToken);
        await EnsureReferencesExistAsync(request.RecipeId, mealTypeId, cancellationToken);
        plan.Assign(mealTypeId, request.RecipeId, request.Servings);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>Retira una asignación.</summary>
    public async Task<DailyPlanResponse> RemoveEntryAsync(
        DateOnly date,
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        var plan = await FindAsync(date, cancellationToken);
        await EnsureMealTypeExistsAsync(mealTypeId, cancellationToken);
        if (plan.RemoveEntry(mealTypeId))
        {
            await plans.SaveChangesAsync(cancellationToken);
        }

        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>Añade un hueco.</summary>
    public async Task<DailyPlanResponse> AddSlotAsync(
        DateOnly date,
        AddMealPlanSlotRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = await FindAsync(date, cancellationToken);
        await EnsureMealTypeExistsAsync(request.MealTypeId, cancellationToken);
        plan.AddSlot(request.MealTypeId);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>Reordena los huecos.</summary>
    public async Task<DailyPlanResponse> ReorderSlotsAsync(
        DateOnly date,
        ReorderMealPlanSlotsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SlotIds);
        var plan = await FindAsync(date, cancellationToken);
        plan.ReorderSlots(request.SlotIds);
        await plans.SaveChangesAsync(cancellationToken);
        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>Retira un hueco.</summary>
    public async Task<DailyPlanResponse> RemoveSlotAsync(
        DateOnly date,
        Guid slotId,
        CancellationToken cancellationToken)
    {
        var plan = await FindAsync(date, cancellationToken);
        if (plan.RemoveSlot(slotId))
        {
            await plans.SaveChangesAsync(cancellationToken);
        }

        return await MapAsync(plan, cancellationToken);
    }

    /// <summary>Cambia la hora prevista de un hueco.</summary>
    public async Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
        DateOnly date,
        Guid slotId,
        SetMealPlanSlotTimeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = await FindAsync(date, cancellationToken);
        var slot = plan.Slots.SingleOrDefault(item => item.Id == slotId) ??
            throw new DomainValidationException(
                "daily-plan.slot.not-found",
                "No se encontró el hueco de comida.");
        var entry = plan.Entries.SingleOrDefault(item => item.MealTypeId == slot.MealTypeId);
        var times = entry is null
            ? new Dictionary<Guid, TimeSpan?>()
            : await references.GetRecipeEstimatedTimesAsync(
                [entry.RecipeId],
                cancellationToken);
        plan.SetSlotTime(slotId, ParsePlannedTime(request.PlannedTime));
        await plans.SaveChangesAsync(cancellationToken);
        return new MealPlanSlotScheduleResponse(
            slot.Id,
            slot.PlannedTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            slot.GetPreparationStartsAt(plan.Date, times.GetValueOrDefault(entry?.RecipeId ?? Guid.Empty)));
    }

    /// <summary>Omite una asignación planificada.</summary>
    public async Task<MealPlanEntryStateResponse> SkipEntryAsync(
        DateOnly date,
        Guid mealTypeId,
        SkipMealPlanEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plan = await FindAsync(date, cancellationToken);
        await EnsureMealTypeExistsAsync(mealTypeId, cancellationToken);
        var entry = plan.SkipEntry(mealTypeId, request.Reason, request.AlternativeDescription);
        await plans.SaveChangesAsync(cancellationToken);
        return new MealPlanEntryStateResponse(
            entry.Id,
            MapState(entry.Status),
            entry.CompletedAt,
            entry.SkippedReason,
            entry.AlternativeDescription);
    }

    private async Task<DailyPlan> FindAsync(
        DateOnly date,
        CancellationToken cancellationToken) =>
        await plans.GetByDateAsync(date, cancellationToken) ??
        throw new DailyPlanNotFoundException(
            "daily-plan.not-found",
            $"No se encontró el plan del {date:yyyy-MM-dd}.");

    private async Task<DailyPlanResponse[]> MapAsync(
        IEnumerable<DailyPlan> source,
        CancellationToken cancellationToken)
    {
        var materialized = source.ToArray();
        var mealTypes = await OrderedMealTypesAsync(cancellationToken);
        var mealTypesById = mealTypes.ToDictionary(item => item.Id);
        var recipeIds = materialized
            .SelectMany(plan => plan.Entries)
            .Select(entry => entry.RecipeId)
            .Distinct()
            .ToArray();
        var estimatedTimes = await references.GetRecipeEstimatedTimesAsync(
            recipeIds,
            cancellationToken);
        return materialized.Select(plan => Map(plan, mealTypesById, estimatedTimes)).ToArray();
    }

    private async Task<DailyPlanResponse> MapAsync(
        DailyPlan plan,
        CancellationToken cancellationToken) =>
        (await MapAsync([plan], cancellationToken))[0];

    private static DailyPlanResponse Map(
        DailyPlan plan,
        Dictionary<Guid, MealType> mealTypes,
        IReadOnlyDictionary<Guid, TimeSpan?> estimatedTimes)
    {
        var entries = plan.Entries.ToDictionary(entry => entry.MealTypeId);
        var meals = plan.Slots
            .OrderBy(slot => slot.Order)
            .Select(slot =>
            {
                var mealType = mealTypes[slot.MealTypeId];
                entries.TryGetValue(slot.MealTypeId, out var entry);
                var estimatedTime = entry is null
                    ? null
                    : estimatedTimes.GetValueOrDefault(entry.RecipeId);
                return new DailyPlanMealResponse(
                    mealType.Id,
                    mealType.Name.Value,
                    mealType.Order,
                    entry?.RecipeId,
                    entry?.Servings ?? 1,
                    entry?.IsCompleted ?? false,
                    entry?.CompletedAt,
                    slot.Id,
                    slot.Order,
                    slot.PlannedTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                    slot.GetPreparationStartsAt(plan.Date, estimatedTime),
                    entry is null ? MealPlanEntryState.Planned : MapState(entry.Status),
                    entry?.SkippedReason,
                    entry?.AlternativeDescription);
            })
            .ToArray();
        return new DailyPlanResponse(plan.Id, plan.Date, meals);
    }

    private async Task<IReadOnlyList<MealType>> OrderedMealTypesAsync(
        CancellationToken cancellationToken) =>
        (await references.ListMealTypesAsync(cancellationToken))
            .OrderBy(mealType => mealType.Order)
            .ThenBy(mealType => mealType.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    private async Task EnsureReferencesExistAsync(
        Guid recipeId,
        Guid mealTypeId,
        CancellationToken cancellationToken)
    {
        if (!await references.RecipeExistsAsync(recipeId, cancellationToken))
        {
            throw new DailyPlanReferenceNotFoundException(
                "daily-plan.recipe.not-found",
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
            throw new DailyPlanReferenceNotFoundException(
                "daily-plan.meal-type.not-found",
                "No se encontró el tipo de comida.");
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
            ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var plannedTime))
        {
            return plannedTime;
        }

        throw new DomainValidationException(
            "daily-plan.slot.planned-time.invalid",
            "La hora prevista no es válida.");
    }

    private static MealPlanEntryState MapState(MealPlanEntryStatus status) => status switch
    {
        MealPlanEntryStatus.Planned => MealPlanEntryState.Planned,
        MealPlanEntryStatus.Completed => MealPlanEntryState.Completed,
        MealPlanEntryStatus.Skipped => MealPlanEntryState.Skipped,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static DailyPlanDateConflictException DuplicateDate(DateOnly date) =>
        new(
            "daily-plan.date.duplicate",
            $"Ya existe un plan para el {date:yyyy-MM-dd}.");
}
