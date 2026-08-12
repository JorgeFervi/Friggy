using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Inventory.Exceptions;
using Friggy.Application.Inventory.Services;
using Friggy.Domain.Catalogs;
using Friggy.Domain.Inventory;
using Friggy.Domain.Recipes;
using Friggy.Domain.WeeklyPlans;
using Friggy.Infrastructure.Persistence;
using Friggy.Infrastructure.Persistence.Repositories;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;

namespace Friggy.IntegrationTests.Persistence;

public sealed class InventoryLotRepositoryTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InventoryLotRepository_SaveAndReload_PreservesLotAndMovements()
    {
        var ingredient = await CreateIngredientAsync();
        var lot = InventoryLot.Create(
            ingredient.Id,
            CatalogSeedIds.Kilogram,
            2.5m,
            new DateOnly(2026, 8, 20),
            OccurredAt);
        lot.Consume(0.5m, OccurredAt.AddMinutes(10));

        await using (var context = Database.CreateDbContext())
        {
            var repository = new InventoryLotRepository(context);
            await repository.AddAsync(lot, TestContext.Current.CancellationToken);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = Database.CreateDbContext();
        var verificationRepository = new InventoryLotRepository(verificationContext);
        var reloaded = await verificationRepository.GetByIdAsync(
            lot.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal(2m, reloaded.Quantity);
        Assert.Equal(
            [InventoryMovementType.InitialStock, InventoryMovementType.Consumption],
            reloaded.Movements.Select(item => item.Type));
        Assert.Equal([2.5m, 2m], reloaded.Movements.Select(item => item.ResultingQuantity));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InventoryLotRepository_ConcurrentConsumption_ReturnsRecoverableConflict()
    {
        var ingredient = await CreateIngredientAsync();
        var lot = InventoryLot.Create(
            ingredient.Id,
            CatalogSeedIds.Kilogram,
            1m,
            new DateOnly(2026, 8, 20),
            OccurredAt);
        await SaveLotAsync(lot);

        await using var firstContext = Database.CreateDbContext();
        await using var secondContext = Database.CreateDbContext();
        var firstRepository = new InventoryLotRepository(firstContext);
        var secondRepository = new InventoryLotRepository(secondContext);
        var first = await firstRepository.GetByIdAsync(
            lot.Id,
            TestContext.Current.CancellationToken);
        var second = await secondRepository.GetByIdAsync(
            lot.Id,
            TestContext.Current.CancellationToken);
        Assert.NotNull(first);
        Assert.NotNull(second);
        first.Consume(0.75m, OccurredAt.AddMinutes(1));
        second.Consume(0.75m, OccurredAt.AddMinutes(2));

        await firstRepository.SaveChangesAsync(TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<InventoryConflictException>(() =>
            secondRepository.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Equal("inventory-lot.concurrency", exception.Code);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteMeal_OneUnitOfWork_PersistsCompletionAndLinkedConsumption()
    {
        var ingredient = await CreateIngredientAsync();
        var recipe = Recipe.Create("Ensalada", TimeSpan.Zero);
        recipe.AddIngredient(ingredient.Id, CatalogSeedIds.Kilogram, 1m, 0);
        recipe.AddStep("Servir", null, 0);
        var plan = WeeklyPlan.Create("Semana", new DateOnly(2026, 8, 10), null);
        plan.Assign(plan.StartDate, CatalogSeedIds.Lunch, recipe.Id, 2);
        var lot = InventoryLot.Create(
            ingredient.Id,
            CatalogSeedIds.Kilogram,
            3m,
            new DateOnly(2026, 8, 20),
            OccurredAt);
        await using (var setup = Database.CreateDbContext())
        {
            setup.AddRange(recipe, plan, lot);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = Database.CreateDbContext())
        {
            var service = new WeeklyPlanInventoryService(
                new WeeklyPlanRepository(context),
                new RecipeRepository(context),
                new InventoryLotRepository(context),
                new InventoryReferenceRepository(context),
                new InventoryUnitOfWork(context),
                new FixedTimeProvider(OccurredAt.AddHours(1)));

            await service.CompleteMealAsync(
                plan.Id,
                plan.StartDate,
                CatalogSeedIds.Lunch,
                new CompleteMealRequest([new(lot.Id, 2m)]),
                TestContext.Current.CancellationToken);
        }

        await using var verification = Database.CreateDbContext();
        var storedEntry = await verification.MealPlanEntries.SingleAsync(
            TestContext.Current.CancellationToken);
        var storedLot = await new InventoryLotRepository(verification).GetByIdAsync(
            lot.Id,
            TestContext.Current.CancellationToken);
        Assert.True(storedEntry.IsCompleted);
        Assert.NotNull(storedLot);
        Assert.Equal(1m, storedLot.Quantity);
        Assert.Equal(storedEntry.Id, storedLot.Movements[^1].MealPlanEntryId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CompleteMeal_ConcurrentRequestsUsingDifferentLots_RollsBackSecondConsumption()
    {
        var ingredient = await CreateIngredientAsync();
        var recipe = Recipe.Create("Crema", TimeSpan.Zero);
        recipe.AddIngredient(ingredient.Id, CatalogSeedIds.Kilogram, 1m, 0);
        recipe.AddStep("Servir", null, 0);
        var plan = WeeklyPlan.Create("Semana concurrente", new DateOnly(2026, 8, 10), null);
        plan.Assign(plan.StartDate, CatalogSeedIds.Lunch, recipe.Id);
        var firstLot = InventoryLot.Create(
            ingredient.Id,
            CatalogSeedIds.Kilogram,
            2m,
            new DateOnly(2026, 8, 20),
            OccurredAt);
        var secondLot = InventoryLot.Create(
            ingredient.Id,
            CatalogSeedIds.Kilogram,
            2m,
            new DateOnly(2026, 8, 21),
            OccurredAt);
        await using (var setup = Database.CreateDbContext())
        {
            setup.AddRange(recipe, plan, firstLot, secondLot);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var firstContext = Database.CreateDbContext();
        await using var secondContext = Database.CreateDbContext();
        var firstPlan = await new WeeklyPlanRepository(firstContext).GetByIdAsync(
            plan.Id,
            TestContext.Current.CancellationToken);
        var secondPlan = await new WeeklyPlanRepository(secondContext).GetByIdAsync(
            plan.Id,
            TestContext.Current.CancellationToken);
        var firstLots = await new InventoryLotRepository(firstContext).ListForUpdateAsync(
            TestContext.Current.CancellationToken);
        var secondLots = await new InventoryLotRepository(secondContext).ListForUpdateAsync(
            TestContext.Current.CancellationToken);
        Assert.NotNull(firstPlan);
        Assert.NotNull(secondPlan);
        var firstEntry = Assert.Single(firstPlan.Entries);
        var secondEntry = Assert.Single(secondPlan.Entries);
        firstLots.Single(item => item.Id == firstLot.Id)
            .Consume(1m, OccurredAt.AddMinutes(1), firstEntry.Id);
        secondLots.Single(item => item.Id == secondLot.Id)
            .Consume(1m, OccurredAt.AddMinutes(2), secondEntry.Id);
        firstPlan.CompleteEntry(plan.StartDate, CatalogSeedIds.Lunch, OccurredAt.AddMinutes(1));
        secondPlan.CompleteEntry(plan.StartDate, CatalogSeedIds.Lunch, OccurredAt.AddMinutes(2));

        await new InventoryUnitOfWork(firstContext).SaveChangesAsync(
            TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<InventoryConflictException>(() =>
            new InventoryUnitOfWork(secondContext).SaveChangesAsync(
                TestContext.Current.CancellationToken));

        Assert.Equal("inventory-lot.concurrency", exception.Code);
        await using var verification = Database.CreateDbContext();
        var storedLots = await new InventoryLotRepository(verification).ListAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(1m, storedLots.Single(item => item.Id == firstLot.Id).Quantity);
        Assert.Equal(2m, storedLots.Single(item => item.Id == secondLot.Id).Quantity);
        Assert.Equal(
            1,
            storedLots.SelectMany(item => item.Movements)
                .Count(item => item.Type == InventoryMovementType.Consumption));
        Assert.True((await verification.MealPlanEntries.SingleAsync(
            TestContext.Current.CancellationToken)).IsCompleted);
    }

    private async Task<Ingredient> CreateIngredientAsync()
    {
        var ingredient = Ingredient.Create($"Ingrediente {Guid.NewGuid():N}");
        await using var context = Database.CreateDbContext();
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return ingredient;
    }

    private async Task SaveLotAsync(InventoryLot lot)
    {
        await using var context = Database.CreateDbContext();
        var repository = new InventoryLotRepository(context);
        await repository.AddAsync(lot, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
