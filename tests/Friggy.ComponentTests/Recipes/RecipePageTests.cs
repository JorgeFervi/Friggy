using Bunit;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Recipes;

public sealed class RecipePageTests : ComponentTest
{
    private static readonly Guid RecipeId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid IngredientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid TagId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid MealTypeId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Category", "Component")]
    public void Recipes_PendingApi_ShowsLoadingThenEmptyState()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<RecipeListItemResponse>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton<IRecipesApiClient>(new StubRecipesApiClient
        {
            PendingList = pending,
        });

        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();

        Assert.Contains("Cargando recetas", component.Markup, StringComparison.Ordinal);
        pending.SetResult([]);
        component.WaitForAssertion(() =>
            Assert.Contains("No hay recetas", component.Markup, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Recipes_ApiUnavailable_ShowsActionableErrorWithoutEmptyState()
    {
        Services.AddSingleton<IRecipesApiClient>(new StubRecipesApiClient
        {
            ListException = new HttpRequestException("La API no está disponible."),
        });

        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains(
                "La API no está disponible",
                component.Find("[role='alert']").TextContent,
                StringComparison.Ordinal);
            Assert.DoesNotContain("No hay recetas", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Recipes_ApiReturnsRecipe_RendersListAndDetailLink()
    {
        Services.AddSingleton<IRecipesApiClient>(new StubRecipesApiClient
        {
            Recipes = [new(RecipeId, "Gazpacho", 20)],
        });

        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Gazpacho", component.Markup, StringComparison.Ordinal);
            Assert.Equal($"recipes/{RecipeId}", component.Find("a[data-testid='recipe-detail']").GetAttribute("href"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeDetails_ExistingRecipe_RendersResolvedCatalogNamesAndOrderedSteps()
    {
        var recipes = new StubRecipesApiClient { Recipe = CompleteRecipe() };
        RegisterApis(recipes);

        var component = Render<global::Friggy.Web.Components.Pages.RecipeDetails>(parameters =>
            parameters.Add(page => page.Id, RecipeId));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Tomate", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Gramo (g)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Vegano", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Comida", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Triturar", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeEdit_ValidNewRecipe_CreatesAndNavigatesToDetail()
    {
        var recipes = new StubRecipesApiClient { Recipe = CompleteRecipe() };
        RegisterApis(recipes);
        var component = Render<global::Friggy.Web.Components.Pages.RecipeEdit>();
        component.WaitForElement("form");

        component.Find("#recipe-name").Change("Gazpacho");
        component.Find("#recipe-estimated-minutes").Change("20");
        component.Find("button[data-action='add-ingredient']").Click();
        component.Find("[data-testid='ingredient-row'] select[data-field='ingredient']").Change(IngredientId.ToString());
        component.Find("[data-testid='ingredient-row'] select[data-field='unit-type']").Change(UnitTypeId.ToString());
        component.Find("[data-testid='ingredient-row'] input[data-field='quantity']").Change("1.5");
        component.Find("button[data-action='add-step']").Click();
        component.Find("[data-testid='step-row'] textarea").Change("Triturar");
        component.Find($"#tag-{TagId}").Change(true);
        component.Find($"#meal-type-{MealTypeId}").Change(true);
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var request = Assert.Single(recipes.Created);
            Assert.Equal("Gazpacho", request.Name);
            Assert.Equal(1.5m, Assert.Single(request.Ingredients).Quantity);
            Assert.Equal("Triturar", Assert.Single(request.Steps).Description);
            Assert.Equal(TagId, Assert.Single(request.TagIds));
            Assert.Equal(MealTypeId, Assert.Single(request.MealTypeIds));
            var navigation = GetRequiredService<NavigationManager>();
            Assert.EndsWith($"/recipes/{RecipeId}", navigation.Uri, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeEdit_ExistingRecipe_LoadsUpdatesAndNavigatesToDetail()
    {
        var recipes = new StubRecipesApiClient { Recipe = CompleteRecipe() };
        RegisterApis(recipes);
        var component = Render<global::Friggy.Web.Components.Pages.RecipeEdit>(parameters =>
            parameters.Add(page => page.Id, RecipeId));
        component.WaitForElement("form");

        Assert.Equal("Gazpacho", component.Find("#recipe-name").GetAttribute("value"));
        Assert.Single(component.FindAll("[data-testid='ingredient-row']"));
        Assert.Single(component.FindAll("[data-testid='step-row']"));
        component.Find("#recipe-name").Change("Salmorejo");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var update = Assert.Single(recipes.Updated);
            Assert.Equal(RecipeId, update.Id);
            Assert.Equal("Salmorejo", update.Request.Name);
            Assert.Equal(1.5m, Assert.Single(update.Request.Ingredients).Quantity);
            var navigation = GetRequiredService<NavigationManager>();
            Assert.EndsWith($"/recipes/{RecipeId}", navigation.Uri, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeEdit_ApiRejectsSave_ShowsErrorAndKeepsFormState()
    {
        var recipes = new StubRecipesApiClient
        {
            Recipe = CompleteRecipe(),
            SaveException = new ApiProblemException("Ya existe una receta con ese nombre."),
        };
        RegisterApis(recipes);
        var component = Render<global::Friggy.Web.Components.Pages.RecipeEdit>();
        component.WaitForElement("form");

        FillMinimalForm(component);
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Ya existe", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal("Gazpacho", component.Find("#recipe-name").GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeEdit_CatalogLoadFails_ShowsErrorWithoutEmptyForm()
    {
        Services.AddSingleton<IRecipesApiClient>(new StubRecipesApiClient());
        Services.AddSingleton<IIngredientsApiClient>(new IngredientsApiClientStub
        {
            ListException = new HttpRequestException("La API no está disponible."),
        });
        Services.AddSingleton<IUnitTypesApiClient>(new UnitTypesApiClientStub());
        Services.AddSingleton<IRecipeTagsApiClient>(new RecipeTagsApiClientStub());
        Services.AddSingleton<IMealTypesApiClient>(new MealTypesApiClientStub());

        var component = Render<global::Friggy.Web.Components.Pages.RecipeEdit>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains(
                "No se pudo cargar el formulario",
                component.Find("[role='alert']").TextContent,
                StringComparison.Ordinal);
            Assert.Empty(component.FindAll("form"));
        });
    }

    private void RegisterApis(StubRecipesApiClient recipes)
    {
        Services.AddSingleton<IRecipesApiClient>(recipes);
        Services.AddSingleton<IIngredientsApiClient>(new IngredientsApiClientStub());
        Services.AddSingleton<IUnitTypesApiClient>(new UnitTypesApiClientStub());
        Services.AddSingleton<IRecipeTagsApiClient>(new RecipeTagsApiClientStub());
        Services.AddSingleton<IMealTypesApiClient>(new MealTypesApiClientStub());
    }

    private static void FillMinimalForm(IRenderedComponent<global::Friggy.Web.Components.Pages.RecipeEdit> component)
    {
        component.Find("#recipe-name").Change("Gazpacho");
        component.Find("#recipe-estimated-minutes").Change("20");
        component.Find("button[data-action='add-ingredient']").Click();
        component.Find("[data-testid='ingredient-row'] select[data-field='ingredient']").Change(IngredientId.ToString());
        component.Find("[data-testid='ingredient-row'] select[data-field='unit-type']").Change(UnitTypeId.ToString());
        component.Find("[data-testid='ingredient-row'] input[data-field='quantity']").Change("1");
        component.Find("button[data-action='add-step']").Click();
        component.Find("[data-testid='step-row'] textarea").Change("Triturar");
    }

    private static RecipeResponse CompleteRecipe() =>
        new(
            RecipeId,
            "Gazpacho",
            20,
            [new(Guid.NewGuid(), IngredientId, UnitTypeId, 1.5m, 0)],
            [new(Guid.NewGuid(), "Triturar", 5, 0)],
            [TagId],
            [MealTypeId]);

    private sealed class StubRecipesApiClient : IRecipesApiClient
    {
        public IReadOnlyList<RecipeListItemResponse> Recipes { get; init; } = [];
        public RecipeResponse? Recipe { get; init; }
        public TaskCompletionSource<IReadOnlyList<RecipeListItemResponse>>? PendingList { get; init; }
        public Exception? ListException { get; init; }
        public ApiProblemException? SaveException { get; init; }
        public List<CreateRecipeRequest> Created { get; } = [];
        public List<(Guid Id, UpdateRecipeRequest Request)> Updated { get; } = [];

        public Task<IReadOnlyList<RecipeListItemResponse>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }

            return PendingList?.Task ?? Task.FromResult(Recipes);
        }

        public Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Recipe ?? throw new InvalidOperationException("Falta configurar la receta."));

        public Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken)
        {
            if (SaveException is not null)
            {
                throw SaveException;
            }

            Created.Add(request);
            return Task.FromResult(Recipe ?? throw new InvalidOperationException("Falta configurar la receta."));
        }

        public Task<RecipeResponse> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken)
        {
            Updated.Add((id, request));
            return Task.FromResult(Recipe ?? throw new InvalidOperationException("Falta configurar la receta."));
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class IngredientsApiClientStub : IIngredientsApiClient
    {
        public Exception? ListException { get; init; }

        public Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }

            return Task.FromResult<IReadOnlyList<IngredientResponse>>([new(IngredientId, "Tomate")]);
        }
        public Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class UnitTypesApiClientStub : IUnitTypesApiClient
    {
        public Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnitTypeResponse>>([new(UnitTypeId, "Gramo", "g")]);
        public Task<UnitTypeResponse> CreateAsync(CreateUnitTypeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<UnitTypeResponse> UpdateAsync(Guid id, UpdateUnitTypeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecipeTagsApiClientStub : IRecipeTagsApiClient
    {
        public Task<IReadOnlyList<RecipeTagResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecipeTagResponse>>([new(TagId, "Vegano")]);
        public Task<RecipeTagResponse> CreateAsync(CreateRecipeTagRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RecipeTagResponse> UpdateAsync(Guid id, UpdateRecipeTagRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class MealTypesApiClientStub : IMealTypesApiClient
    {
        public Task<IReadOnlyList<MealTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MealTypeResponse>>([new(MealTypeId, "Comida", 1)]);
        public Task<MealTypeResponse> CreateAsync(CreateMealTypeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MealTypeResponse> UpdateAsync(Guid id, UpdateMealTypeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
