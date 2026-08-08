using Friggy.Application.Catalogs;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.Ingredients.Interfaces;
using Friggy.Application.Catalogs.Ingredients.Services;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Tests.Catalogs;

public sealed class IngredientServiceTests
{
    [Fact]
    public async Task Create_ValidName_PersistsAndReturnsIngredient()
    {
        var repository = new FakeIngredientRepository();
        var service = new IngredientService(repository);

        var result = await service.CreateAsync(
            new CreateIngredientRequest("  Tomate  "),
            TestContext.Current.CancellationToken);

        Assert.Equal("Tomate", result.Name);
        Assert.Contains(repository.Items, item => item.Id == result.Id);
    }

    [Fact]
    public async Task Create_DuplicateNormalizedName_ThrowsCatalogConflictException()
    {
        var repository = new FakeIngredientRepository();
        repository.Items.Add(Ingredient.Create("Tomate"));
        var service = new IngredientService(repository);

        var exception = await Assert.ThrowsAsync<CatalogConflictException>(() =>
            service.CreateAsync(
                new CreateIngredientRequest(" tomate "),
                TestContext.Current.CancellationToken));

        Assert.Equal("ingredient.name.duplicate", exception.Code);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task Update_MissingIngredient_ThrowsCatalogNotFoundException()
    {
        var service = new IngredientService(new FakeIngredientRepository());

        var exception = await Assert.ThrowsAsync<CatalogNotFoundException>(() =>
            service.UpdateAsync(
                Guid.NewGuid(),
                new UpdateIngredientRequest("Cebolla"),
                TestContext.Current.CancellationToken));

        Assert.Equal("ingredient.not-found", exception.Code);
    }

    [Fact]
    public async Task List_UnorderedItems_ReturnsItemsOrderedByName()
    {
        var repository = new FakeIngredientRepository();
        repository.Items.Add(Ingredient.Create("Tomate"));
        repository.Items.Add(Ingredient.Create("Cebolla"));
        var service = new IngredientService(repository);

        var result = await service.ListAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            result,
            item => Assert.Equal("Cebolla", item.Name),
            item => Assert.Equal("Tomate", item.Name));
    }

    private sealed class FakeIngredientRepository : IIngredientRepository
    {
        public List<Ingredient> Items { get; } = [];

        public Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(ingredient);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNormalizedNameAsync(
            string normalizedName,
            Guid? excludingId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(item =>
                item.Name.Normalized == normalizedName && item.Id != excludingId));

        public Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

        public Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Ingredient>>(Items);

        public void Remove(Ingredient ingredient) => Items.Remove(ingredient);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
