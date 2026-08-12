using Friggy.Domain.Catalogs;

namespace Friggy.Domain.WeeklyPlans;

public sealed class WeeklyPlan
{
    private const int DaysInWeek = 7;
    private readonly List<MealPlanEntry> entries = [];
    private readonly List<MealPlanSlot> slots = [];

    private WeeklyPlan()
    {
        Name = null!;
    }

    private WeeklyPlan(
        Guid id,
        CatalogName name,
        DateOnly startDate,
        string? description)
    {
        Id = id;
        Name = name;
        StartDate = startDate;
        Description = description;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate => StartDate.AddDays(DaysInWeek - 1);

    public string? Description { get; private set; }

    public IReadOnlyList<MealPlanEntry> Entries => entries.AsReadOnly();

    public IReadOnlyList<MealPlanSlot> Slots => slots.AsReadOnly();

    public IReadOnlyList<DateOnly> Dates => Enumerable
        .Range(0, DaysInWeek)
        .Select(StartDate.AddDays)
        .ToArray();

    public static WeeklyPlan Create(
        string? name,
        DateOnly startDate,
        string? description)
    {
        var planName = CatalogName.Create(name, "weekly-plan.name.required");
        if (startDate.DayOfWeek is not DayOfWeek.Monday)
        {
            throw new DomainValidationException(
                "weekly-plan.start-date.monday",
                "La semana debe comenzar en lunes.");
        }

        return new WeeklyPlan(
            Guid.NewGuid(),
            planName,
            startDate,
            NormalizeDescription(description));
    }

    public void Assign(
        DateOnly date,
        Guid mealTypeId,
        Guid recipeId,
        int servings = 1)
    {
        ValidateDate(date);
        ValidateRequiredId(mealTypeId, "weekly-plan.entry.meal-type-id.required");
        ValidateRequiredId(recipeId, "weekly-plan.entry.recipe-id.required");
        if (servings <= 0)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.servings.positive",
                "Las raciones deben ser mayores que cero.");
        }

        var existing = entries.SingleOrDefault(entry =>
            entry.Date == date && entry.MealTypeId == mealTypeId);
        if (existing?.IsCompleted is true)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.completed",
                "No se puede modificar una comida completada.");
        }

        EnsureSlot(date, mealTypeId);
        if (existing is null)
        {
            entries.Add(MealPlanEntry.Create(Id, date, mealTypeId, recipeId, servings));
        }
        else
        {
            existing.Replace(recipeId, servings);
        }
    }

    public MealPlanSlot AddSlot(DateOnly date, Guid mealTypeId)
    {
        ValidateDate(date, "weekly-plan.slot.date.out-of-range");
        ValidateRequiredId(mealTypeId, "weekly-plan.slot.meal-type-id.required");
        if (slots.Any(slot => slot.Date == date && slot.MealTypeId == mealTypeId))
        {
            throw new DomainValidationException(
                "weekly-plan.slot.meal-type.duplicate",
                "El tipo de comida ya existe en este día.");
        }

        var order = slots.Count(slot => slot.Date == date);
        var slot = MealPlanSlot.Create(Id, date, mealTypeId, order);
        slots.Add(slot);
        return slot;
    }

    public void ReorderSlots(DateOnly date, IReadOnlyList<Guid> orderedSlotIds)
    {
        ArgumentNullException.ThrowIfNull(orderedSlotIds);
        ValidateDate(date, "weekly-plan.slot.date.out-of-range");
        var daySlots = slots
            .Where(slot => slot.Date == date)
            .ToArray();
        var uniqueIds = orderedSlotIds.Distinct().ToArray();
        if (orderedSlotIds.Count != daySlots.Length ||
            uniqueIds.Length != orderedSlotIds.Count ||
            uniqueIds.Any(id => daySlots.All(slot => slot.Id != id)))
        {
            throw new DomainValidationException(
                "weekly-plan.slot.order.invalid",
                "El orden debe incluir una vez todos los huecos del día.");
        }

        var slotsById = daySlots.ToDictionary(slot => slot.Id);
        for (var order = 0; order < orderedSlotIds.Count; order++)
        {
            slotsById[orderedSlotIds[order]].MoveTo(order);
        }
    }

    public bool RemoveSlot(Guid slotId)
    {
        ValidateRequiredId(slotId, "weekly-plan.slot.id.required");
        var slot = slots.SingleOrDefault(item => item.Id == slotId);
        if (slot is null)
        {
            return false;
        }

        var assignment = entries.SingleOrDefault(entry =>
            entry.Date == slot.Date && entry.MealTypeId == slot.MealTypeId);
        if (assignment?.IsCompleted is true)
        {
            throw new DomainValidationException(
                "weekly-plan.slot.completed",
                "No se puede retirar el hueco de una comida completada.");
        }

        if (assignment is not null)
        {
            throw new DomainValidationException(
                "weekly-plan.slot.assigned",
                "Retira la receta asignada antes de eliminar el hueco.");
        }

        slots.Remove(slot);
        CompactSlotOrder(slot.Date);
        return true;
    }

    public MealPlanSlot SetSlotTime(Guid slotId, TimeOnly? plannedTime)
    {
        ValidateRequiredId(slotId, "weekly-plan.slot.id.required");
        var slot = slots.SingleOrDefault(item => item.Id == slotId) ??
            throw new DomainValidationException(
                "weekly-plan.slot.not-found",
                "No se encontró el hueco de comida.");
        slot.SetPlannedTime(plannedTime);
        return slot;
    }

    public bool RemoveEntry(DateOnly date, Guid mealTypeId)
    {
        ValidateDate(date);
        ValidateRequiredId(mealTypeId, "weekly-plan.entry.meal-type-id.required");

        var existing = entries.SingleOrDefault(entry =>
            entry.Date == date && entry.MealTypeId == mealTypeId);
        if (existing?.IsCompleted is true)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.completed",
                "No se puede retirar una comida completada.");
        }

        return existing is not null && entries.Remove(existing);
    }

    public MealPlanEntry CompleteEntry(
        DateOnly date,
        Guid mealTypeId,
        DateTimeOffset completedAt)
    {
        ValidateDate(date);
        ValidateRequiredId(mealTypeId, "weekly-plan.entry.meal-type-id.required");
        var entry = entries.SingleOrDefault(item =>
            item.Date == date && item.MealTypeId == mealTypeId) ??
            throw new DomainValidationException(
                "weekly-plan.entry.not-assigned",
                "No hay una receta asignada a la comida.");
        entry.Complete(completedAt);
        return entry;
    }

    public void UpdateDetails(string? name, string? description)
    {
        var updatedName = CatalogName.Create(name, "weekly-plan.name.required");
        var updatedDescription = NormalizeDescription(description);

        Name = updatedName;
        Description = updatedDescription;
    }

    private static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private MealPlanSlot EnsureSlot(DateOnly date, Guid mealTypeId) =>
        slots.SingleOrDefault(slot =>
            slot.Date == date && slot.MealTypeId == mealTypeId) ??
        AddSlot(date, mealTypeId);

    private void CompactSlotOrder(DateOnly date)
    {
        var daySlots = slots
            .Where(slot => slot.Date == date)
            .OrderBy(slot => slot.Order)
            .ToArray();
        for (var order = 0; order < daySlots.Length; order++)
        {
            daySlots[order].MoveTo(order);
        }
    }

    private void ValidateDate(
        DateOnly date,
        string code = "weekly-plan.entry.date.out-of-range")
    {
        if (date < StartDate || date > EndDate)
        {
            throw new DomainValidationException(
                code,
                "La fecha debe pertenecer a la semana.");
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
