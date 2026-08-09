using Friggy.Domain.WeeklyPlans;

namespace Friggy.Application.WeeklyPlans.Interfaces;

public interface IWeeklyPlanRepository
{
    Task<IReadOnlyList<WeeklyPlan>> ListAsync(CancellationToken cancellationToken);

    Task<WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken);

    void Remove(WeeklyPlan plan);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
