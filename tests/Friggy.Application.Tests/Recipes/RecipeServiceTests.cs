using Friggy.Application.Recipes.Dtos;
using Friggy.Application.Recipes.Exceptions;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Application.Recipes.Services;
using Friggy.Domain.Catalogs;
using Friggy.Domain.Recipes;

namespace Friggy.Application.Tests.Recipes;

public sealed class RecipeServiceTests
{
    [Fact]
    public async Task Create_ValidRequest_PersistsAndReturnsOrderedRecipe()
    {
        var scenario = RecipeScenario.Create();
        var service = scenario.CreateService();

        var result = await service.CreateAsync(
            scenario.Request,
            TestContext.Current.CancellationToken);

        var persisted = Assert.Single(scenario.Recipes.Items);
        Assert.Equal(result.Id, persisted.Id);
        Assert.Equal("Gazpacho", result.Name);
        Assert.Equal(20, result.EstimatedMinutes);
        Assert.Collection(
            result.Ingredients,
            item =>
            {
                Assert.Equal(scenario.IngredientLineOneId, item.Id);
                Assert.Equal(0, item.Order);
            },
            item =>
            {
                Assert.Equal(scenario.IngredientLineTwoId, item.Id);
                Assert.Equal(1, item.Order);
            });
        Assert.Collection(
            result.Steps,
            item =>
            {
                Assert.Equal(0, item.Order);
                Assert.Equal([scenario.IngredientLineOneId], item.RecipeIngredientIds);
            },
            item =>
            {
                Assert.Equal(1, item.Order);
                Assert.Equal(
                    [scenario.IngredientLineOneId, scenario.IngredientLineTwoId],
                    item.RecipeIngredientIds);
            });
        Assert.Equal(
            [scenario.IngredientLineOneId],
            persisted.Steps.Single(item => item.Order == 0).RecipeIngredientIds);
        Assert.Equal(scenario.Request.TagIds, result.TagIds);
        Assert.Equal(scenario.Request.MealTypeIds, result.MealTypeIds);
        Assert.Equal(1, scenario.Recipes.SaveCount);
    }

    [Theory]
    [InlineData("ingredient", "recipe.ingredient.not-found")]
    [InlineData("unit-type", "recipe.unit-type.not-found")]
    [InlineData("tag", "recipe.tag.not-found")]
    [InlineData("meal-type", "recipe.meal-type.not-found")]
    public async Task Create_MissingCatalogReference_ThrowsAndDoesNotPersist(
        string missingReference,
        string expectedCode)
    {
        var scenario = RecipeScenario.Create();
        scenario.References.RemoveAll(missingReference);
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeReferenceNotFoundException>(() =>
            service.CreateAsync(scenario.Request, TestContext.Current.CancellationToken));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Empty(scenario.Recipes.Items);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Create_DuplicateNormalizedName_ThrowsAndDoesNotPersist()
    {
        var scenario = RecipeScenario.Create();
        scenario.Recipes.Items.Add(BuildRecipe("GAZPACHO"));
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeNameConflictException>(() =>
            service.CreateAsync(scenario.Request, TestContext.Current.CancellationToken));

        Assert.Equal("recipe.name.duplicate", exception.Code);
        Assert.Single(scenario.Recipes.Items);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Create_DuplicateTag_ThrowsDomainConflictAndDoesNotPersist()
    {
        var scenario = RecipeScenario.Create();
        var duplicateTagRequest = scenario.Request with
        {
            TagIds = [scenario.TagId, scenario.TagId],
        };
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeConflictException>(() =>
            service.CreateAsync(duplicateTagRequest, TestContext.Current.CancellationToken));

        Assert.Equal("recipe.tag.duplicate", exception.Code);
        Assert.Empty(scenario.Recipes.Items);
    }

    [Fact]
    public async Task Create_StepReferencesUnknownIngredientLine_ThrowsAndDoesNotPersist()
    {
        var scenario = RecipeScenario.Create();
        var invalidRequest = scenario.Request with
        {
            Steps =
            [
                scenario.Request.Steps[0] with
                {
                    RecipeIngredientIds = [Guid.NewGuid()],
                },
                scenario.Request.Steps[1],
            ],
        };
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.CreateAsync(invalidRequest, TestContext.Current.CancellationToken));

        Assert.Equal("recipe-ingredient.not-found", exception.Code);
        Assert.Empty(scenario.Recipes.Items);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Create_DuplicateStepAssociation_ThrowsAndDoesNotPersist()
    {
        var scenario = RecipeScenario.Create();
        var invalidRequest = scenario.Request with
        {
            Steps =
            [
                scenario.Request.Steps[0] with
                {
                    RecipeIngredientIds =
                    [
                        scenario.IngredientLineOneId,
                        scenario.IngredientLineOneId,
                    ],
                },
                scenario.Request.Steps[1],
            ],
        };
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeConflictException>(() =>
            service.CreateAsync(invalidRequest, TestContext.Current.CancellationToken));

        Assert.Equal("recipe-step.ingredient.duplicate", exception.Code);
        Assert.Empty(scenario.Recipes.Items);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Create_DuplicateIngredientLineIdentity_ThrowsAndDoesNotPersist()
    {
        var scenario = RecipeScenario.Create();
        var invalidRequest = scenario.Request with
        {
            Ingredients = scenario.Request.Ingredients
                .Select(item => item with { Id = scenario.IngredientLineOneId })
                .ToArray(),
        };
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeConflictException>(() =>
            service.CreateAsync(invalidRequest, TestContext.Current.CancellationToken));

        Assert.Equal("recipe-ingredient.id.duplicate", exception.Code);
        Assert.Empty(scenario.Recipes.Items);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Create_CancelledToken_PropagatesCancellationAndDoesNotPersist()
    {
        var scenario = RecipeScenario.Create();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var service = scenario.CreateService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CreateAsync(scenario.Request, cancellation.Token));

        Assert.Empty(scenario.Recipes.Items);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Update_ValidRequest_ReplacesAggregateAndPreservesIdentity()
    {
        var scenario = RecipeScenario.Create();
        var existing = BuildRecipe("Anterior");
        scenario.Recipes.Items.Add(existing);
        var service = scenario.CreateService();

        var result = await service.UpdateAsync(
            existing.Id,
            new UpdateRecipeRequest(
                scenario.Request.Name,
                scenario.Request.EstimatedMinutes,
                scenario.Request.Ingredients,
                scenario.Request.Steps,
                scenario.Request.TagIds,
                scenario.Request.MealTypeIds),
            TestContext.Current.CancellationToken);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal("Gazpacho", existing.Name.Value);
        Assert.Equal(
            [scenario.IngredientLineOneId, scenario.IngredientLineTwoId],
            existing.Ingredients.OrderBy(item => item.Order).Select(item => item.Id));
        Assert.Equal(
            [scenario.IngredientLineOneId],
            existing.Steps.Single(item => item.Order == 0).RecipeIngredientIds);
        Assert.Equal(2, existing.Steps.Count);
        Assert.Equal(1, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Update_ReorderedIngredientLines_PreservesStepAssociationsByIdentity()
    {
        var scenario = RecipeScenario.Create();
        var existing = BuildRecipe("Anterior");
        scenario.Recipes.Items.Add(existing);
        var reorderedRequest = new UpdateRecipeRequest(
            scenario.Request.Name,
            scenario.Request.EstimatedMinutes,
            scenario.Request.Ingredients
                .Select(item => item with { Order = item.Order == 0 ? 1 : 0 })
                .ToArray(),
            scenario.Request.Steps,
            scenario.Request.TagIds,
            scenario.Request.MealTypeIds);
        var service = scenario.CreateService();

        var result = await service.UpdateAsync(
            existing.Id,
            reorderedRequest,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [scenario.IngredientLineTwoId, scenario.IngredientLineOneId],
            result.Ingredients.Select(item => item.Id));
        Assert.Equal(
            [scenario.IngredientLineOneId],
            result.Steps.Single(item => item.Order == 0).RecipeIngredientIds);
        Assert.Equal(
            [scenario.IngredientLineTwoId, scenario.IngredientLineOneId],
            result.Steps.Single(item => item.Order == 1).RecipeIngredientIds);
    }

    [Fact]
    public async Task Update_ExistingIngredientLineIdentity_ReusesTrackedLineAndUpdatesItsValues()
    {
        var scenario = RecipeScenario.Create();
        var existing = BuildRecipe("Anterior");
        var trackedIngredient = existing.Ingredients[0];
        scenario.References.IngredientIds.Add(trackedIngredient.IngredientId);
        scenario.References.UnitTypeIds.Add(trackedIngredient.UnitTypeId);
        scenario.Recipes.Items.Add(existing);
        var request = new UpdateRecipeRequest(
            "Actualizada",
            30,
            [
                new RecipeIngredientRequest(
                    trackedIngredient.IngredientId,
                    trackedIngredient.UnitTypeId,
                    3m,
                    0,
                    trackedIngredient.Id),
            ],
            [
                new RecipeStepRequest(
                    "Preparar de nuevo",
                    10,
                    0,
                    [trackedIngredient.Id]),
            ],
            [],
            []);
        var service = scenario.CreateService();

        var result = await service.UpdateAsync(
            existing.Id,
            request,
            TestContext.Current.CancellationToken);

        var updatedIngredient = Assert.Single(existing.Ingredients);
        Assert.Same(trackedIngredient, updatedIngredient);
        Assert.Equal(3m, updatedIngredient.Quantity);
        Assert.Equal(trackedIngredient.Id, Assert.Single(result.Ingredients).Id);
        Assert.Equal(
            [trackedIngredient.Id],
            Assert.Single(result.Steps).RecipeIngredientIds);
    }

    [Fact]
    public async Task Update_InvalidStepAssociation_ThrowsAndPreservesExistingAggregate()
    {
        var scenario = RecipeScenario.Create();
        var existing = BuildRecipe("Anterior");
        existing.AssignIngredientToStep(existing.Steps[0].Id, existing.Ingredients[0].Id);
        var originalIngredientId = existing.Ingredients[0].Id;
        var originalStepId = existing.Steps[0].Id;
        scenario.Recipes.Items.Add(existing);
        var invalidRequest = new UpdateRecipeRequest(
            scenario.Request.Name,
            scenario.Request.EstimatedMinutes,
            scenario.Request.Ingredients,
            [
                scenario.Request.Steps[0] with
                {
                    RecipeIngredientIds = [Guid.NewGuid()],
                },
                scenario.Request.Steps[1],
            ],
            scenario.Request.TagIds,
            scenario.Request.MealTypeIds);
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.UpdateAsync(
                existing.Id,
                invalidRequest,
                TestContext.Current.CancellationToken));

        Assert.Equal("recipe-ingredient.not-found", exception.Code);
        Assert.Equal("Anterior", existing.Name.Value);
        Assert.Equal(originalIngredientId, Assert.Single(existing.Ingredients).Id);
        Assert.Equal(originalStepId, Assert.Single(existing.Steps).Id);
        Assert.Equal([originalIngredientId], existing.Steps[0].RecipeIngredientIds);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Update_InvalidReference_ThrowsAndPreservesExistingAggregate()
    {
        var scenario = RecipeScenario.Create();
        var existing = BuildRecipe("Anterior");
        var ingredientId = existing.Ingredients[0].Id;
        scenario.Recipes.Items.Add(existing);
        scenario.References.UnitTypeIds.Clear();
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeReferenceNotFoundException>(() =>
            service.UpdateAsync(
                existing.Id,
                new UpdateRecipeRequest(
                    scenario.Request.Name,
                    scenario.Request.EstimatedMinutes,
                    scenario.Request.Ingredients,
                    scenario.Request.Steps,
                    scenario.Request.TagIds,
                    scenario.Request.MealTypeIds),
                TestContext.Current.CancellationToken));

        Assert.Equal("recipe.unit-type.not-found", exception.Code);
        Assert.Equal("Anterior", existing.Name.Value);
        Assert.Equal(ingredientId, Assert.Single(existing.Ingredients).Id);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Update_DuplicateName_ThrowsAndPreservesExistingAggregate()
    {
        var scenario = RecipeScenario.Create();
        var existing = BuildRecipe("Anterior");
        scenario.Recipes.Items.Add(existing);
        scenario.Recipes.Items.Add(BuildRecipe("Gazpacho"));
        var originalIngredientId = existing.Ingredients[0].Id;
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeNameConflictException>(() =>
            service.UpdateAsync(
                existing.Id,
                new UpdateRecipeRequest(
                    scenario.Request.Name,
                    scenario.Request.EstimatedMinutes,
                    scenario.Request.Ingredients,
                    scenario.Request.Steps,
                    scenario.Request.TagIds,
                    scenario.Request.MealTypeIds),
                TestContext.Current.CancellationToken));

        Assert.Equal("recipe.name.duplicate", exception.Code);
        Assert.Equal("Anterior", existing.Name.Value);
        Assert.Equal(originalIngredientId, Assert.Single(existing.Ingredients).Id);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Get_MissingRecipe_ThrowsNotFound()
    {
        var scenario = RecipeScenario.Create();
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeNotFoundException>(() =>
            service.GetAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Equal("recipe.not-found", exception.Code);
    }

    [Fact]
    public async Task Get_ExistingRecipe_ReturnsCompleteOrderedResponse()
    {
        var scenario = RecipeScenario.Create();
        var service = scenario.CreateService();
        var created = await service.CreateAsync(
            scenario.Request,
            TestContext.Current.CancellationToken);

        var result = await service.GetAsync(
            created.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.Name, result.Name);
        Assert.Equal(created.EstimatedMinutes, result.EstimatedMinutes);
        Assert.Equal(created.Ingredients, result.Ingredients);
        Assert.Equal(created.Steps.Count, result.Steps.Count);
        for (var index = 0; index < created.Steps.Count; index++)
        {
            Assert.Equal(created.Steps[index].Id, result.Steps[index].Id);
            Assert.Equal(created.Steps[index].Description, result.Steps[index].Description);
            Assert.Equal(created.Steps[index].EstimatedMinutes, result.Steps[index].EstimatedMinutes);
            Assert.Equal(created.Steps[index].Order, result.Steps[index].Order);
            Assert.Equal(
                created.Steps[index].RecipeIngredientIds,
                result.Steps[index].RecipeIngredientIds);
        }

        Assert.Equal(created.TagIds, result.TagIds);
        Assert.Equal(created.MealTypeIds, result.MealTypeIds);
        Assert.Equal([0, 1], result.Ingredients.Select(item => item.Order));
        Assert.Equal([0, 1], result.Steps.Select(item => item.Order));
    }

    [Fact]
    public async Task List_UnorderedRecipes_ReturnsSummariesOrderedByName()
    {
        var scenario = RecipeScenario.Create();
        scenario.Recipes.Items.Add(BuildRecipe("Tortilla"));
        scenario.Recipes.Items.Add(BuildRecipe("Gazpacho"));
        var service = scenario.CreateService();

        var result = await service.ListAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            result,
            item => Assert.Equal("Gazpacho", item.Name),
            item => Assert.Equal("Tortilla", item.Name));
    }

    [Fact]
    public async Task Delete_ExistingRecipe_RemovesAndSaves()
    {
        var scenario = RecipeScenario.Create();
        var recipe = BuildRecipe("Gazpacho");
        scenario.Recipes.Items.Add(recipe);
        var service = scenario.CreateService();

        await service.DeleteAsync(recipe.Id, TestContext.Current.CancellationToken);

        Assert.Empty(scenario.Recipes.Items);
        Assert.Equal(1, scenario.Recipes.SaveCount);
    }

    [Fact]
    public async Task Delete_MissingRecipe_ThrowsAndDoesNotSave()
    {
        var scenario = RecipeScenario.Create();
        var service = scenario.CreateService();

        var exception = await Assert.ThrowsAsync<RecipeNotFoundException>(() =>
            service.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Equal("recipe.not-found", exception.Code);
        Assert.Equal(0, scenario.Recipes.SaveCount);
    }

    private static Recipe BuildRecipe(string name)
    {
        var recipe = Recipe.Create(name, TimeSpan.FromMinutes(20));
        recipe.AddIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m, 0);
        recipe.AddStep("Preparar", null, 0);
        recipe.EnsureComplete();
        return recipe;
    }

    private sealed class RecipeScenario
    {
        private RecipeScenario(
            FakeRecipeRepository recipes,
            FakeRecipeCatalogRepository references,
            CreateRecipeRequest request,
            Guid tagId,
            Guid ingredientLineOneId,
            Guid ingredientLineTwoId)
        {
            Recipes = recipes;
            References = references;
            Request = request;
            TagId = tagId;
            IngredientLineOneId = ingredientLineOneId;
            IngredientLineTwoId = ingredientLineTwoId;
        }

        public FakeRecipeRepository Recipes { get; }

        public FakeRecipeCatalogRepository References { get; }

        public CreateRecipeRequest Request { get; }

        public Guid TagId { get; }

        public Guid IngredientLineOneId { get; }

        public Guid IngredientLineTwoId { get; }

        public static RecipeScenario Create()
        {
            var ingredientOne = Guid.NewGuid();
            var ingredientTwo = Guid.NewGuid();
            var unitType = Guid.NewGuid();
            var tag = Guid.NewGuid();
            var mealType = Guid.NewGuid();
            var ingredientLineOne = Guid.NewGuid();
            var ingredientLineTwo = Guid.NewGuid();
            var references = new FakeRecipeCatalogRepository();
            references.IngredientIds.UnionWith([ingredientOne, ingredientTwo]);
            references.UnitTypeIds.Add(unitType);
            references.TagIds.Add(tag);
            references.MealTypeIds.Add(mealType);

            var request = new CreateRecipeRequest(
                " Gazpacho ",
                20,
                [
                    new RecipeIngredientRequest(
                        ingredientTwo,
                        unitType,
                        2m,
                        1,
                        ingredientLineTwo),
                    new RecipeIngredientRequest(
                        ingredientOne,
                        unitType,
                        1m,
                        0,
                        ingredientLineOne),
                ],
                [
                    new RecipeStepRequest(
                        "Servir",
                        null,
                        1,
                        [ingredientLineTwo, ingredientLineOne]),
                    new RecipeStepRequest("Triturar", 5, 0, [ingredientLineOne]),
                ],
                [tag],
                [mealType]);

            return new RecipeScenario(
                new FakeRecipeRepository(),
                references,
                request,
                tag,
                ingredientLineOne,
                ingredientLineTwo);
        }

        public RecipeService CreateService() => new(Recipes, References);
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        public List<Recipe> Items { get; } = [];

        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Recipe>>(Items);
        }

        public Task<IReadOnlyList<RecipeListItemResponse>> ListSummariesAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<RecipeListItemResponse>>(
                Items.Select(item => new RecipeListItemResponse(
                    item.Id,
                    item.Name.Value,
                    checked((int)item.EstimatedTime.TotalMinutes))).ToArray());
        }

        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<bool> ExistsByNormalizedNameAsync(
            string normalizedName,
            Guid? excludingId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.Any(item =>
                item.Name.Normalized == normalizedName && item.Id != excludingId));
        }

        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(recipe);
            return Task.CompletedTask;
        }

        public void Remove(Recipe recipe) => Items.Remove(recipe);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRecipeCatalogRepository : IRecipeCatalogRepository
    {
        public HashSet<Guid> IngredientIds { get; } = [];

        public HashSet<Guid> UnitTypeIds { get; } = [];

        public HashSet<Guid> TagIds { get; } = [];

        public HashSet<Guid> MealTypeIds { get; } = [];

        public void RemoveAll(string reference)
        {
            switch (reference)
            {
                case "ingredient":
                    IngredientIds.Clear();
                    break;
                case "unit-type":
                    UnitTypeIds.Clear();
                    break;
                case "tag":
                    TagIds.Clear();
                    break;
                case "meal-type":
                    MealTypeIds.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reference), reference, null);
            }
        }

        public Task<bool> IngredientsExistAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            ContainsAllAsync(IngredientIds, ids, cancellationToken);

        public Task<bool> UnitTypesExistAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            ContainsAllAsync(UnitTypeIds, ids, cancellationToken);

        public Task<bool> TagsExistAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            ContainsAllAsync(TagIds, ids, cancellationToken);

        public Task<bool> MealTypesExistAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            ContainsAllAsync(MealTypeIds, ids, cancellationToken);

        private static Task<bool> ContainsAllAsync(
            HashSet<Guid> existing,
            IReadOnlyCollection<Guid> requested,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(requested.All(existing.Contains));
        }
    }
}
