using Friggy.Application.DailyPlans.Exceptions;
using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;
using Friggy.Domain.Recipes;
using Friggy.Infrastructure.Persistence.Repositories;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;

namespace Friggy.IntegrationTests.Persistence;

public sealed class DailyPlanRepositoryTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private static readonly DateOnly PlannedDate = new(2026, 8, 4);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAndReload_PreservesDailyAggregateWithoutRedundantDates()
    {
        var recipe = await CreateRecipeAsync("Gazpacho");
        var plan = DailyPlan.Create(PlannedDate);
        var slot = plan.AddSlot(CatalogSeedIds.Lunch);
        plan.Assign(CatalogSeedIds.Lunch, recipe.Id, 3);
        plan.SetSlotTime(slot.Id, new TimeOnly(14, 0));
        await SaveAsync(plan);

        await using var context = Database.CreateDbContext();
        var reloaded = await new DailyPlanRepository(context).GetByDateAsync(
            PlannedDate,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal(PlannedDate, reloaded.Date);
        Assert.Equal(3, Assert.Single(reloaded.Entries).Servings);
        Assert.Equal(new TimeOnly(14, 0), Assert.Single(reloaded.Slots).PlannedTime);
        Assert.DoesNotContain(context.ChangeTracker.Entries(), entry =>
            entry.State != EntityState.Unchanged);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ListBetween_InclusiveRange_FiltersAndOrdersBeforeLoadingChildren()
    {
        await SaveAsync(DailyPlan.Create(PlannedDate.AddDays(1)));
        await SaveAsync(DailyPlan.Create(PlannedDate.AddDays(-1)));
        await SaveAsync(DailyPlan.Create(PlannedDate));
        await using var context = Database.CreateDbContext();

        var result = await new DailyPlanRepository(context).ListBetweenAsync(
            PlannedDate,
            PlannedDate.AddDays(1),
            TestContext.Current.CancellationToken);

        Assert.Equal([PlannedDate, PlannedDate.AddDays(1)], result.Select(plan => plan.Date));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SameDate_ConcurrentInsert_TranslatesUniqueViolationToDateConflict()
    {
        await using var firstContext = Database.CreateDbContext();
        await using var secondContext = Database.CreateDbContext();
        var first = new DailyPlanRepository(firstContext);
        var second = new DailyPlanRepository(secondContext);
        await first.AddAsync(DailyPlan.Create(PlannedDate), TestContext.Current.CancellationToken);
        await second.AddAsync(DailyPlan.Create(PlannedDate), TestContext.Current.CancellationToken);
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<DailyPlanDateConflictException>(() =>
            second.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Equal("daily-plan.date.duplicate", exception.Code);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReferenceRepository_BatchesRecipeTimesAndOrdersMealTypes()
    {
        var first = await CreateRecipeAsync("Primera", TimeSpan.FromMinutes(10));
        var second = await CreateRecipeAsync("Segunda", TimeSpan.FromMinutes(20));
        await using var context = Database.CreateDbContext();
        var repository = new DailyPlanReferenceRepository(context);

        var times = await repository.GetRecipeEstimatedTimesAsync(
            [first.Id, second.Id],
            TestContext.Current.CancellationToken);
        var mealTypes = await repository.ListMealTypesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(10), times[first.Id]);
        Assert.Equal(TimeSpan.FromMinutes(20), times[second.Id]);
        Assert.Equal([0, 1, 2], mealTypes.Select(item => item.Order));
    }

    private async Task<Recipe> CreateRecipeAsync(string name, TimeSpan? estimatedTime = null)
    {
        var recipe = Recipe.Create(name, estimatedTime ?? TimeSpan.Zero);
        await using var context = Database.CreateDbContext();
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return recipe;
    }

    private async Task SaveAsync(DailyPlan plan)
    {
        await using var context = Database.CreateDbContext();
        var repository = new DailyPlanRepository(context);
        await repository.AddAsync(plan, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
