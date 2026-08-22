using Friggy.Application.DailyPlanTemplates.Interfaces;
using Friggy.Domain.DailyPlanTemplates;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class DailyPlanTemplateRepository(FriggyDbContext context) : IDailyPlanTemplateRepository
{
    public async Task<IReadOnlyList<DailyPlanTemplate>> ListAsync(CancellationToken cancellationToken) =>
        await CompleteQuery().AsNoTrackingWithIdentityResolution()
            .OrderBy(item => item.Name.Normalized).ToListAsync(cancellationToken);

    public Task<DailyPlanTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task AddAsync(DailyPlanTemplate planTemplate, CancellationToken cancellationToken) =>
        await context.DailyPlanTemplates.AddAsync(planTemplate, cancellationToken);

    public void Remove(DailyPlanTemplate planTemplate) => context.DailyPlanTemplates.Remove(planTemplate);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    private IQueryable<DailyPlanTemplate> CompleteQuery() =>
        context.DailyPlanTemplates.Include(item => item.Meals.OrderBy(meal => meal.Order));
}
