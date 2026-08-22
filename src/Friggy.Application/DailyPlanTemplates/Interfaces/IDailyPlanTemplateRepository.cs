using Friggy.Domain.DailyPlanTemplates;

namespace Friggy.Application.DailyPlanTemplates.Interfaces;

/// <summary>Puerto de persistencia de plantillas de planes diarios.</summary>
public interface IDailyPlanTemplateRepository
{
    Task<IReadOnlyList<DailyPlanTemplate>> ListAsync(CancellationToken cancellationToken);

    Task<DailyPlanTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(DailyPlanTemplate planTemplate, CancellationToken cancellationToken);

    void Remove(DailyPlanTemplate planTemplate);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
