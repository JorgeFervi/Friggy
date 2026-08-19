using Friggy.Domain.Catalogs;

namespace Friggy.Domain.WeeklyPlans;

/// <summary>
/// Clase <see cref="WeeklyPlan"/> que representa la planificación de comidas
/// de una semana completa.
/// </summary>
public sealed class WeeklyPlan
{
    /// <summary>
    /// Número de días que contiene un plan semanal.
    /// </summary>
    private const int DaysInWeek = 7;
    private readonly List<MealPlanEntry> entries = [];
    private readonly List<MealPlanSlot> slots = [];

    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private WeeklyPlan()
    {
        Name = null!;
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar al plan de forma interna.
    /// </param>
    /// <param name="name">
    /// Nombre del plan semanal.
    /// </param>
    /// <param name="startDate">
    /// Fecha de inicio del plan semanal.
    /// </param>
    /// <param name="description">
    /// Descripción opcional del plan semanal.
    /// </param>
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

    /// <summary>
    /// Código único para identificar al plan de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre del plan semanal.
    /// </summary>
    public CatalogName Name { get; private set; }

    /// <summary>
    /// Fecha de inicio del plan semanal.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Fecha de finalización del plan semanal.
    /// </summary>
    public DateOnly EndDate => StartDate.AddDays(DaysInWeek - 1);

    /// <summary>
    /// Descripción opcional del plan semanal.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Asignaciones de recetas del plan semanal.
    /// </summary>
    public IReadOnlyList<MealPlanEntry> Entries => entries.AsReadOnly();

    /// <summary>
    /// Huecos de comida definidos para el plan semanal.
    /// </summary>
    public IReadOnlyList<MealPlanSlot> Slots => slots.AsReadOnly();

    /// <summary>
    /// Fechas de todos los días incluidos en el plan semanal.
    /// </summary>
    public IReadOnlyList<DateOnly> Dates => Enumerable
        .Range(0, DaysInWeek)
        .Select(StartDate.AddDays)
        .ToArray();

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="name">
    /// Nombre del plan semanal.
    /// </param>
    /// <param name="startDate">
    /// Fecha de inicio del plan semanal, que debe ser lunes.
    /// </param>
    /// <param name="description">
    /// Descripción opcional del plan semanal.
    /// </param>
    /// <returns>
    /// Objeto <see cref="WeeklyPlan"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el nombre está vacío o la fecha
    /// de inicio no corresponde a un lunes.
    /// </exception>
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

    /// <summary>
    /// Método que asigna una receta a un tipo de comida de un día del plan.
    /// </summary>
    /// <param name="date">
    /// Día al que se asigna la receta.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <param name="recipeId">
    /// Código de la receta.
    /// </param>
    /// <param name="servings">
    /// Número de raciones de la receta.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha, los identificadores o
    /// las raciones no son válidos, o la comida no puede modificarse.
    /// </exception>
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

        if (existing?.IsSkipped is true)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.skipped",
                "No se puede modificar una comida omitida.");
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

    /// <summary>
    /// Método que añade un hueco de comida a un día del plan.
    /// </summary>
    /// <param name="date">
    /// Día al que se añade el hueco.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <returns>
    /// Hueco añadido al plan.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha o el identificador no
    /// son válidos, o el tipo de comida ya existe en el día.
    /// </exception>
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

    /// <summary>
    /// Método que establece el orden de los huecos de un día.
    /// </summary>
    /// <param name="date">
    /// Día cuyos huecos se van a ordenar.
    /// </param>
    /// <param name="orderedSlotIds">
    /// Códigos de los huecos en el orden deseado.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando la lista de identificadores es nula.
    /// </exception>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha no es válida o la lista
    /// no contiene exactamente todos los huecos del día una sola vez.
    /// </exception>
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

    /// <summary>
    /// Método que retira un hueco de comida del plan.
    /// </summary>
    /// <param name="slotId">
    /// Código del hueco que se va a retirar.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si el hueco existía y se retiró; en caso contrario,
    /// <see langword="false"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador no es válido o el
    /// hueco tiene una comida que impide retirarlo.
    /// </exception>
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

        if (assignment?.IsSkipped is true)
        {
            throw new DomainValidationException(
                "weekly-plan.slot.skipped",
                "No se puede retirar el hueco de una comida omitida.");
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

    /// <summary>
    /// Método que establece la hora prevista de un hueco de comida.
    /// </summary>
    /// <param name="slotId">
    /// Código del hueco que se va a actualizar.
    /// </param>
    /// <param name="plannedTime">
    /// Hora prevista para preparar la comida.
    /// </param>
    /// <returns>
    /// Hueco actualizado.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador no es válido o
    /// no se encuentra el hueco.
    /// </exception>
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

    /// <summary>
    /// Método que retira una asignación de receta del plan.
    /// </summary>
    /// <param name="date">
    /// Día de la asignación.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la asignación existía y se retiró; en caso
    /// contrario, <see langword="false"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha o el identificador no
    /// son válidos, o la comida no puede retirarse.
    /// </exception>
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

        if (existing?.IsSkipped is true)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.skipped",
                "No se puede retirar una comida omitida.");
        }

        return existing is not null && entries.Remove(existing);
    }

    /// <summary>
    /// Método que marca como completada una comida asignada al plan.
    /// </summary>
    /// <param name="date">
    /// Día de la asignación.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <param name="completedAt">
    /// Fecha y hora en la que se completó la comida.
    /// </param>
    /// <returns>
    /// Asignación completada.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha o el identificador no
    /// son válidos, no hay receta asignada o el estado impide completarla.
    /// </exception>
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

    /// <summary>
    /// Método que marca como omitida una comida asignada al plan.
    /// </summary>
    /// <param name="date">
    /// Día de la asignación.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <param name="reason">
    /// Motivo de la omisión.
    /// </param>
    /// <param name="alternativeDescription">
    /// Descripción opcional de la alternativa usada.
    /// </param>
    /// <returns>
    /// Asignación omitida.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha o el identificador no
    /// son válidos, no hay receta asignada o el estado impide omitirla.
    /// </exception>
    public MealPlanEntry SkipEntry(
        DateOnly date,
        Guid mealTypeId,
        string? reason,
        string? alternativeDescription)
    {
        ValidateDate(date);
        ValidateRequiredId(mealTypeId, "weekly-plan.entry.meal-type-id.required");
        var entry = entries.SingleOrDefault(item =>
            item.Date == date && item.MealTypeId == mealTypeId) ??
            throw new DomainValidationException(
                "weekly-plan.entry.not-assigned",
                "No hay una receta asignada a la comida.");
        entry.Skip(reason, alternativeDescription);
        return entry;
    }

    /// <summary>
    /// Método que actualiza el nombre y la descripción del plan semanal.
    /// </summary>
    /// <param name="name">
    /// Nuevo nombre del plan semanal.
    /// </param>
    /// <param name="description">
    /// Nueva descripción opcional del plan semanal.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el nombre está vacío.
    /// </exception>
    public void UpdateDetails(string? name, string? description)
    {
        var updatedName = CatalogName.Create(name, "weekly-plan.name.required");
        var updatedDescription = NormalizeDescription(description);

        Name = updatedName;
        Description = updatedDescription;
    }

    /// <summary>
    /// Método que normaliza una descripción opcional eliminando espacios
    /// exteriores.
    /// </summary>
    /// <param name="description">
    /// Descripción que se va a normalizar.
    /// </param>
    /// <returns>
    /// Descripción normalizada o <see langword="null"/> cuando está vacía.
    /// </returns>
    private static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Método que obtiene o crea el hueco correspondiente a un día y tipo de comida.
    /// </summary>
    /// <param name="date">
    /// Día del hueco.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <returns>
    /// Hueco existente o creado.
    /// </returns>
    private MealPlanSlot EnsureSlot(DateOnly date, Guid mealTypeId) =>
        slots.SingleOrDefault(slot =>
            slot.Date == date && slot.MealTypeId == mealTypeId) ??
        AddSlot(date, mealTypeId);

    /// <summary>
    /// Método que compacta el orden de los huecos restantes de un día.
    /// </summary>
    /// <param name="date">
    /// Día cuyos huecos se van a reordenar.
    /// </param>
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

    /// <summary>
    /// Método que comprueba que una fecha pertenece al plan semanal.
    /// </summary>
    /// <param name="date">
    /// Fecha que se va a validar.
    /// </param>
    /// <param name="code">
    /// Código de error que se lanzará si la fecha no es válida.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la fecha no pertenece a la semana.
    /// </exception>
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

    /// <summary>
    /// Método que comprueba que un identificador sea obligatorio.
    /// </summary>
    /// <param name="id">
    /// Identificador que se va a validar.
    /// </param>
    /// <param name="code">
    /// Código de error que se lanzará si el identificador no es válido.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío.
    /// </exception>
    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }
}
