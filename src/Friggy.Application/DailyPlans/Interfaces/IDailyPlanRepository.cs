using Friggy.Domain.DailyPlans;

namespace Friggy.Application.DailyPlans.Interfaces;

/// <summary>Puerto de persistencia del agregado diario.</summary>
public interface IDailyPlanRepository
{
    /// <summary>Lista los planes existentes dentro de un intervalo inclusivo.</summary>
    Task<IReadOnlyList<DailyPlan>> ListBetweenAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    /// <summary>Busca el plan de una fecha.</summary>
    Task<DailyPlan?> GetByDateAsync(DateOnly plannedDate, CancellationToken cancellationToken);

    /// <summary>Lista las fechas solicitadas que ya tienen un plan.</summary>
    Task<IReadOnlyList<DateOnly>> ListExistingDatesAsync(
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken);

    /// <summary>Añade un plan.</summary>
    Task AddAsync(DailyPlan plan, CancellationToken cancellationToken);

    /// <summary>Añade varios planes para guardarlos como una única unidad.</summary>
    Task AddRangeAsync(IReadOnlyCollection<DailyPlan> plans, CancellationToken cancellationToken);

    /// <summary>Marca un plan para eliminación.</summary>
    void Remove(DailyPlan plan);

    /// <summary>Guarda los cambios pendientes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
