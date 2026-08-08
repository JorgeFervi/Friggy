using Friggy.Domain.Catalogs;
using Friggy.Infrastructure.Persistence.Repositories;
using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;

namespace Friggy.IntegrationTests.Persistence;

public sealed class CatalogRepositoryTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task IngredientRepository_SaveAndReload_PreservesCatalogValue()
    {
        var ingredient = Ingredient.Create("Tomate");
        await using (var context = Database.CreateDbContext())
        {
            var repository = new IngredientRepository(context);
            await repository.AddAsync(ingredient, TestContext.Current.CancellationToken);
            await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verificationContext = Database.CreateDbContext();
        var verificationRepository = new IngredientRepository(verificationContext);
        var reloaded = await verificationRepository.GetByIdAsync(
            ingredient.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal("Tomate", reloaded.Name.Value);
        Assert.Equal("TOMATE", reloaded.Name.Normalized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IngredientRepository_DuplicateNormalizedName_DatabaseRejectsDuplicate()
    {
        await using var context = Database.CreateDbContext();
        context.Ingredients.Add(Ingredient.Create("Tomate"));
        context.Ingredients.Add(Ingredient.Create(" tomate "));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SeedData_NewDatabase_ContainsUnitsAndMealTypes()
    {
        await using var context = Database.CreateDbContext();

        var units = await context.UnitTypes.ToListAsync(TestContext.Current.CancellationToken);
        var mealTypes = await context.MealTypes.ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(7, units.Count);
        Assert.Equal(3, mealTypes.Count);
    }
}
