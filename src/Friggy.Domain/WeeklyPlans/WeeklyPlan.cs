using Friggy.Domain.Catalogs;

namespace Friggy.Domain.WeeklyPlans;

public sealed class WeeklyPlan
{
    private const int DaysInWeek = 7;
    private readonly List<MealPlanEntry> entries = [];

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
        if (existing is null)
        {
            entries.Add(MealPlanEntry.Create(Id, date, mealTypeId, recipeId, servings));
            return;
        }

        if (existing.IsCompleted)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.completed",
                "No se puede modificar una comida completada.");
        }

        existing.Replace(recipeId, servings);
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

    private void ValidateDate(DateOnly date)
    {
        if (date < StartDate || date > EndDate)
        {
            throw new DomainValidationException(
                "weekly-plan.entry.date.out-of-range",
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
