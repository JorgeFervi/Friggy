using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Domain.WeeklyPlans;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class WeeklyPlanRepository(FriggyDbContext context) : IWeeklyPlanRepository
{
    public async Task<IReadOnlyList<WeeklyPlan>> ListAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cancellationToken);

    public Task<WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(plan => plan.Id == id, cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        context.WeeklyPlans.AnyAsync(
            plan => plan.Name.Normalized == normalizedName &&
                (!excludingId.HasValue || plan.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(WeeklyPlan plan, CancellationToken cancellationToken) =>
        await context.WeeklyPlans.AddAsync(plan, cancellationToken);

    public void Remove(WeeklyPlan plan) => context.WeeklyPlans.Remove(plan);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);

    private IQueryable<WeeklyPlan> CompleteQuery() =>
        context.WeeklyPlans
            .Include(plan => plan.Entries
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.MealTypeId));
}
