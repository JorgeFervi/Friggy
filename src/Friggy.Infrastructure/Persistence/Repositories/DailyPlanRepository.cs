using Friggy.Application.DailyPlans.Exceptions;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Domain.DailyPlans;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>Repositorio EF Core del agregado diario.</summary>
public sealed class DailyPlanRepository(FriggyDbContext context) : IDailyPlanRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyPlan>> ListBetweenAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken) =>
        await CompleteQuery()
            .AsNoTrackingWithIdentityResolution()
            .Where(plan => plan.Date >= startDate && plan.Date <= endDate)
            .OrderBy(plan => plan.Date)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<DailyPlan?> GetByDateAsync(
        DateOnly plannedDate,
        CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(
            plan => plan.Date == plannedDate,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateOnly>> ListExistingDatesAsync(
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken) =>
        await context.DailyPlans
            .AsNoTracking()
            .Where(plan => dates.Contains(plan.Date))
            .OrderBy(plan => plan.Date)
            .Select(plan => plan.Date)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(DailyPlan plan, CancellationToken cancellationToken) =>
        await context.DailyPlans.AddAsync(plan, cancellationToken);

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IReadOnlyCollection<DailyPlan> plans,
        CancellationToken cancellationToken) =>
        await context.DailyPlans.AddRangeAsync(plans, cancellationToken);

    /// <inheritdoc />
    public void Remove(DailyPlan plan) => context.DailyPlans.Remove(plan);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var reorderedSlots = context.ChangeTracker
            .Entries<MealPlanSlot>()
            .Where(entry =>
                entry.State == EntityState.Modified &&
                entry.Property(slot => slot.Order).IsModified)
            .ToArray();
        try
        {
            if (reorderedSlots.Length == 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            await SaveReorderedSlotsAsync(reorderedSlots, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDateDuplicate(exception))
        {
            throw new DailyPlanDateConflictException(
                "daily-plan.date.duplicate",
                "Ya existe un plan para la fecha indicada.");
        }
    }

    private async Task SaveReorderedSlotsAsync(
        IReadOnlyCollection<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<MealPlanSlot>>
            reorderedSlots,
        CancellationToken cancellationToken)
    {
        var deletedSlots = context.ChangeTracker
            .Entries<MealPlanSlot>()
            .Where(entry => entry.State == EntityState.Deleted)
            .ToArray();
        var temporarySlots = reorderedSlots.Concat(deletedSlots).ToArray();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        for (var index = 0; index < temporarySlots.Length; index++)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE meal_plan_slots
                SET "order" = {int.MaxValue - index}
                WHERE "Id" = {temporarySlots[index].Entity.Id}
                """,
                cancellationToken);
        }

        foreach (var entry in reorderedSlots)
        {
            var property = entry.Property(slot => slot.Order);
            var finalOrder = property.CurrentValue;
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE meal_plan_slots
                SET "order" = {finalOrder}
                WHERE "Id" = {entry.Entity.Id}
                """,
                cancellationToken);
            property.OriginalValue = finalOrder;
            property.IsModified = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<DailyPlan> CompleteQuery() =>
        context.DailyPlans
            .Include(plan => plan.Slots.OrderBy(slot => slot.Order))
            .Include(plan => plan.Entries.OrderBy(entry => entry.MealTypeId))
            .AsSplitQuery();

    private static bool IsDateDuplicate(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_daily_plans_date",
        };
}
