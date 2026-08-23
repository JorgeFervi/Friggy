using Friggy.Application.Catalogs;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.MealTypes.Interfaces;
using Friggy.Application.Catalogs.MealTypes.Services;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Interfaces;
using Friggy.Application.Catalogs.RecipeTags.Services;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Application.Catalogs.UnitTypes.Services;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Tests.Catalogs;

public sealed class OtherCatalogServicesTests
{
    [Fact]
    public async Task UnitType_CreateDuplicateName_ThrowsConflict()
    {
        var repository = new FakeUnitTypeRepository(UnitType.Create("Gramo", "g"));
        var service = new UnitTypeService(repository);

        var exception = await Assert.ThrowsAsync<CatalogConflictException>(() =>
            service.CreateAsync(new(" gramo ", "gr"), TestContext.Current.CancellationToken));

        Assert.Equal("unit-type.name.duplicate", exception.Code);
    }

    [Fact]
    public async Task UnitType_CreateConvertible_ReturnsMeasurementMetadata()
    {
        var repository = new FakeUnitTypeRepository(UnitType.Create(
            "Gramo", "g", MeasurementDimension.Mass, 1m, true, true));
        var service = new UnitTypeService(repository);
        var request = new CreateUnitTypeRequest("Kilogramo", "kg")
        {
            MeasurementDimension = "mass",
            BaseUnitFactor = 1000m,
            CanUseForCooking = true,
            CanUseForShopping = true,
        };

        var result = await service.CreateAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("mass", result.MeasurementDimension);
        Assert.Equal(1000m, result.BaseUnitFactor);
        Assert.True(result.CanUseForCooking);
        Assert.True(result.CanUseForShopping);
    }

    [Fact]
    public async Task UnitType_UpdateReferencedFactor_ThrowsConflict()
    {
        var unit = UnitType.Create("Gramo", "g", MeasurementDimension.Mass, 1m, true, true);
        var repository = new FakeUnitTypeRepository(unit) { ReferencedId = unit.Id };
        var service = new UnitTypeService(repository);
        var request = new UpdateUnitTypeRequest("Gramo", "g") { BaseUnitFactor = 10m };

        var exception = await Assert.ThrowsAsync<CatalogConflictException>(() =>
            service.UpdateAsync(unit.Id, request, TestContext.Current.CancellationToken));

        Assert.Equal("unit-type.conversion-metadata.in-use", exception.Code);
        Assert.Equal(1m, unit.BaseUnitFactor);
    }

    [Fact]
    public async Task RecipeTag_CreateValidName_ReturnsTag()
    {
        var repository = new FakeRecipeTagRepository();
        var service = new RecipeTagService(repository);

        var result = await service.CreateAsync(
            new("Vegano"),
            TestContext.Current.CancellationToken);

        Assert.Equal("Vegano", result.Name);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task MealType_List_ReturnsOrderThenName()
    {
        var repository = new FakeMealTypeRepository(
            MealType.Create("Cena", 2),
            MealType.Create("Comida", 1),
            MealType.Create("Almuerzo", 1));
        var service = new MealTypeService(repository);

        var result = await service.ListAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            result,
            item => Assert.Equal("Almuerzo", item.Name),
            item => Assert.Equal("Comida", item.Name),
            item => Assert.Equal("Cena", item.Name));
    }

    private sealed class FakeUnitTypeRepository(params UnitType[] items) : IUnitTypeRepository
    {
        private readonly List<UnitType> values = [.. items];
        public Guid? ReferencedId { get; init; }
        public Task AddAsync(UnitType item, CancellationToken cancellationToken) { values.Add(item); return Task.CompletedTask; }
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(values.Any(x => x.Name.Normalized == name && x.Id != excludingId));
        public Task<UnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(values.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<UnitType>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<UnitType>>(values);
        public Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(ReferencedId == id);
        public void Remove(UnitType item) => values.Remove(item);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRecipeTagRepository : IRecipeTagRepository
    {
        public List<RecipeTag> Items { get; } = [];
        public Task AddAsync(RecipeTag item, CancellationToken cancellationToken) { Items.Add(item); return Task.CompletedTask; }
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(Items.Any(x => x.Name.Normalized == name && x.Id != excludingId));
        public Task<RecipeTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<RecipeTag>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RecipeTag>>(Items);
        public void Remove(RecipeTag item) => Items.Remove(item);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMealTypeRepository(params MealType[] items) : IMealTypeRepository
    {
        private readonly List<MealType> values = [.. items];
        public Task AddAsync(MealType item, CancellationToken cancellationToken) { values.Add(item); return Task.CompletedTask; }
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken) => Task.FromResult(values.Any(x => x.Name.Normalized == name && x.Id != excludingId));
        public Task<MealType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(values.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<MealType>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MealType>>(values);
        public void Remove(MealType item) => values.Remove(item);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
