using Friggy.Domain.Catalogs;

namespace Friggy.Domain.DailyPlans;

/// <summary>
/// Representa la planificación de comidas de una única fecha.
/// </summary>
public sealed class DailyPlan
{
    private readonly List<MealPlanEntry> entries = [];
    private readonly List<MealPlanSlot> slots = [];

    private DailyPlan()
    {
    }

    private DailyPlan(Guid id, DateOnly date)
    {
        Id = id;
        Date = date;
    }

    /// <summary>Código técnico del plan.</summary>
    public Guid Id { get; private set; }

    /// <summary>Fecha única planificada.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Recetas asignadas al día.</summary>
    public IReadOnlyList<MealPlanEntry> Entries => entries.AsReadOnly();

    /// <summary>Huecos de comida configurados para el día.</summary>
    public IReadOnlyList<MealPlanSlot> Slots => slots.AsReadOnly();

    /// <summary>Crea un plan para cualquier fecha válida.</summary>
    public static DailyPlan Create(DateOnly date) => new(Guid.NewGuid(), date);

    /// <summary>Asigna una receta a un tipo de comida.</summary>
    public void Assign(Guid mealTypeId, Guid recipeId, int servings = 1)
    {
        ValidateRequiredId(mealTypeId, "daily-plan.entry.meal-type-id.required");
        ValidateRequiredId(recipeId, "daily-plan.entry.recipe-id.required");
        if (servings <= 0)
        {
            throw new DomainValidationException(
                "daily-plan.entry.servings.positive",
                "Las raciones deben ser mayores que cero.");
        }

        var existing = entries.SingleOrDefault(entry => entry.MealTypeId == mealTypeId);
        EnsureEntryCanChange(existing);
        EnsureSlot(mealTypeId);
        if (existing is null)
        {
            entries.Add(MealPlanEntry.Create(Id, mealTypeId, recipeId, servings));
        }
        else
        {
            existing.Replace(recipeId, servings);
        }
    }

    /// <summary>Añade un hueco de comida.</summary>
    public MealPlanSlot AddSlot(Guid mealTypeId)
    {
        ValidateRequiredId(mealTypeId, "daily-plan.slot.meal-type-id.required");
        if (slots.Any(slot => slot.MealTypeId == mealTypeId))
        {
            throw new DomainValidationException(
                "daily-plan.slot.meal-type.duplicate",
                "El tipo de comida ya existe en este día.");
        }

        var slot = MealPlanSlot.Create(Id, mealTypeId, slots.Count);
        slots.Add(slot);
        return slot;
    }

    /// <summary>Reordena todos los huecos del día.</summary>
    public void ReorderSlots(IReadOnlyList<Guid> orderedSlotIds)
    {
        ArgumentNullException.ThrowIfNull(orderedSlotIds);
        var uniqueIds = orderedSlotIds.Distinct().ToArray();
        if (orderedSlotIds.Count != slots.Count ||
            uniqueIds.Length != orderedSlotIds.Count ||
            uniqueIds.Any(id => slots.All(slot => slot.Id != id)))
        {
            throw new DomainValidationException(
                "daily-plan.slot.order.invalid",
                "El orden debe incluir una vez todos los huecos del día.");
        }

        var slotsById = slots.ToDictionary(slot => slot.Id);
        for (var order = 0; order < orderedSlotIds.Count; order++)
        {
            slotsById[orderedSlotIds[order]].MoveTo(order);
        }
    }

    /// <summary>Retira un hueco que no tenga una asignación.</summary>
    public bool RemoveSlot(Guid slotId)
    {
        ValidateRequiredId(slotId, "daily-plan.slot.id.required");
        var slot = slots.SingleOrDefault(item => item.Id == slotId);
        if (slot is null)
        {
            return false;
        }

        if (entries.Any(entry => entry.MealTypeId == slot.MealTypeId))
        {
            throw new DomainValidationException(
                "daily-plan.slot.assigned",
                "Retira la receta asignada antes de eliminar el hueco.");
        }

        slots.Remove(slot);
        CompactSlotOrder();
        return true;
    }

    /// <summary>Establece la hora prevista de un hueco.</summary>
    public MealPlanSlot SetSlotTime(Guid slotId, TimeOnly? plannedTime)
    {
        ValidateRequiredId(slotId, "daily-plan.slot.id.required");
        var slot = slots.SingleOrDefault(item => item.Id == slotId) ??
            throw new DomainValidationException(
                "daily-plan.slot.not-found",
                "No se encontró el hueco de comida.");
        slot.SetPlannedTime(plannedTime);
        return slot;
    }

    /// <summary>Retira una asignación planificada.</summary>
    public bool RemoveEntry(Guid mealTypeId)
    {
        ValidateRequiredId(mealTypeId, "daily-plan.entry.meal-type-id.required");
        var existing = entries.SingleOrDefault(entry => entry.MealTypeId == mealTypeId);
        EnsureEntryCanChange(existing);
        return existing is not null && entries.Remove(existing);
    }

    /// <summary>Marca una comida como completada.</summary>
    public MealPlanEntry CompleteEntry(Guid mealTypeId, DateTimeOffset completedAt)
    {
        var entry = GetAssignedEntry(mealTypeId);
        entry.Complete(completedAt);
        return entry;
    }

    /// <summary>Marca una comida como omitida.</summary>
    public MealPlanEntry SkipEntry(
        Guid mealTypeId,
        string? reason,
        string? alternativeDescription)
    {
        var entry = GetAssignedEntry(mealTypeId);
        entry.Skip(reason, alternativeDescription);
        return entry;
    }

    private MealPlanEntry GetAssignedEntry(Guid mealTypeId)
    {
        ValidateRequiredId(mealTypeId, "daily-plan.entry.meal-type-id.required");
        return entries.SingleOrDefault(item => item.MealTypeId == mealTypeId) ??
            throw new DomainValidationException(
                "daily-plan.entry.not-assigned",
                "No hay una receta asignada a la comida.");
    }

    private MealPlanSlot EnsureSlot(Guid mealTypeId) =>
        slots.SingleOrDefault(slot => slot.MealTypeId == mealTypeId) ?? AddSlot(mealTypeId);

    private void CompactSlotOrder()
    {
        var orderedSlots = slots.OrderBy(slot => slot.Order).ToArray();
        for (var order = 0; order < orderedSlots.Length; order++)
        {
            orderedSlots[order].MoveTo(order);
        }
    }

    private static void EnsureEntryCanChange(MealPlanEntry? entry)
    {
        if (entry?.IsCompleted is true)
        {
            throw new DomainValidationException(
                "daily-plan.entry.completed",
                "No se puede modificar una comida completada.");
        }

        if (entry?.IsSkipped is true)
        {
            throw new DomainValidationException(
                "daily-plan.entry.skipped",
                "No se puede modificar una comida omitida.");
        }
    }

    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }
}
