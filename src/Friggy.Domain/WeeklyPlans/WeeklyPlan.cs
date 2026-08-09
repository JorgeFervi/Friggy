using Friggy.Domain.Catalogs;

namespace Friggy.Domain.WeeklyPlans;

public sealed class WeeklyPlan
{
    private const int DaysInWeek = 7;

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

    private static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
