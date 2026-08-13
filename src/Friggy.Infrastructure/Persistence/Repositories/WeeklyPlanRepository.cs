using Friggy.Application.WeeklyPlans.Dtos;
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

    public async Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListSummariesAsync(
        CancellationToken cancellationToken)
    {
        var items = await context.WeeklyPlans
            .AsNoTracking()
            .OrderBy(plan => plan.StartDate)
            .ThenBy(plan => plan.Name.Normalized)
            .Select(plan => new
            {
                plan.Id,
                Name = plan.Name.Value,
                plan.StartDate,
            })
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new WeeklyPlanListItemResponse(
                item.Id,
                item.Name,
                item.StartDate,
                item.StartDate.AddDays(6)))
            .ToArray();
    }

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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var reorderedSlots = context.ChangeTracker
            .Entries<MealPlanSlot>()
            .Where(entry =>
                entry.State == EntityState.Modified &&
                entry.Property(slot => slot.Order).IsModified)
            .ToArray();
        if (reorderedSlots.Length == 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var deletedSlots = context.ChangeTracker
            .Entries<MealPlanSlot>()
            .Where(entry => entry.State == EntityState.Deleted)
            .ToArray();
        var slotsRequiringTemporaryOrder = reorderedSlots
            .Concat(deletedSlots)
            .ToArray();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        for (var index = 0; index < slotsRequiringTemporaryOrder.Length; index++)
        {
            var slotId = slotsRequiringTemporaryOrder[index].Entity.Id;
            var temporaryOrder = int.MaxValue - index;
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE meal_plan_slots
                SET "order" = {temporaryOrder}
                WHERE "Id" = {slotId}
                """,
                cancellationToken);
        }

        foreach (var entry in reorderedSlots)
        {
            var orderProperty = entry.Property(slot => slot.Order);
            var finalOrder = orderProperty.CurrentValue;
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE meal_plan_slots
                SET "order" = {finalOrder}
                WHERE "Id" = {entry.Entity.Id}
                """,
                cancellationToken);
            orderProperty.OriginalValue = finalOrder;
            orderProperty.IsModified = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<WeeklyPlan> CompleteQuery() =>
        context.WeeklyPlans
            .Include(plan => plan.Slots
                .OrderBy(slot => slot.Date)
                .ThenBy(slot => slot.Order))
            .Include(plan => plan.Entries
                .OrderBy(entry => entry.Date)
                .ThenBy(entry => entry.MealTypeId))
            .AsSplitQuery();
}
