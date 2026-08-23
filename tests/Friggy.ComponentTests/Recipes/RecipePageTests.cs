using System.Net;
using System.Net.Http.Json;
using Bunit;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Recipes;

public sealed class RecipePageTests : ComponentTest
{
    private static readonly Guid RecipeId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid IngredientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondUnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid TagId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid MealTypeId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid RecipeIngredientId = Guid.Parse("60000000-0000-0000-0000-000000000001");

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
    public void Recipes_FiltersByNameIngredientAndClassificationAndSortsBothWays()
    {
        var onionId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var dinnerId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var recipes = new StubRecipesApiClient
        {
            Recipes =
            [
                new(RecipeId, "Gazpacho", 20, [IngredientId], [TagId], [MealTypeId]),
                new(Guid.Parse("50000000-0000-0000-0000-000000000002"), "Sopa de cebolla", 45, [onionId], [], [dinnerId]),
                new(Guid.Parse("50000000-0000-0000-0000-000000000003"), "Ensalada", 10, [IngredientId, onionId], [TagId], [dinnerId]),
            ],
        };
        Services.AddSingleton<IRecipesApiClient>(recipes);
        Services.AddSingleton<IIngredientsApiClient>(new IngredientsApiClientStub
        {
            Items = [new(IngredientId, "Tomate"), new(onionId, "Cebolla")],
        });
        Services.AddSingleton<IRecipeTagsApiClient>(new RecipeTagsApiClientStub());
        Services.AddSingleton<IMealTypesApiClient>(new MealTypesApiClientStub
        {
            Items = [new(MealTypeId, "Comida", 1), new(dinnerId, "Cena", 2)],
        });

        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();
        component.WaitForElement("[data-testid='recipe-filters']");

        component.Find("#recipe-name-filter").Input("sopa");
        Assert.Equal(["Sopa de cebolla"], VisibleRecipeNames(component));

        component.Find("#recipe-name-filter").Input(string.Empty);
        component.Find($"input[data-filter-ingredient='{IngredientId}']").Change(true);
        Assert.Equal(["Ensalada", "Gazpacho"], VisibleRecipeNames(component));

        component.Find($"input[data-filter-classification='meal-type:{dinnerId}']").Change(true);
        Assert.Equal(["Ensalada"], VisibleRecipeNames(component));

        component.Find("button[data-action='clear-recipe-filters']").Click();
        component.Find("#recipe-sort").Change("time-desc");
        Assert.Equal(["Sopa de cebolla", "Gazpacho", "Ensalada"], VisibleRecipeNames(component));

        component.Find("#recipe-sort").Change("time-asc");
        Assert.Equal(["Ensalada", "Gazpacho", "Sopa de cebolla"], VisibleRecipeNames(component));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Recipes_DeleteConfirmed_DeletesOnceAndRemovesRecipeFromList()
    {
        var recipes = new StubRecipesApiClient
        {
            Recipes = [new(RecipeId, "Gazpacho", 20)],
        };
        Services.AddSingleton<IRecipesApiClient>(recipes);
        SetupConfirmDialog();
        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();
        component.WaitForElement("button[data-action='delete-recipe']");

        component.Find("button[data-action='delete-recipe']").Click();
        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(RecipeId, Assert.Single(recipes.Deleted));
            Assert.DoesNotContain("Gazpacho", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Recipes_DeleteCancelled_KeepsRecipeWithoutCallingApi()
    {
        var recipes = new StubRecipesApiClient
        {
            Recipes = [new(RecipeId, "Gazpacho", 20)],
        };
        Services.AddSingleton<IRecipesApiClient>(recipes);
        SetupConfirmDialog();
        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();
        component.WaitForElement("button[data-action='delete-recipe']");

        component.Find("button[data-action='delete-recipe']").Click();
        component.Find(".confirm-dialog__actions button.friggy-button--secondary").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Empty(recipes.Deleted);
            Assert.Contains("Gazpacho", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Recipes_DeletePending_DisablesActionAndPreventsDuplicateRequest()
    {
        var pendingDelete = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var recipes = new StubRecipesApiClient
        {
            Recipes = [new(RecipeId, "Gazpacho", 20)],
            PendingDelete = pendingDelete,
        };
        Services.AddSingleton<IRecipesApiClient>(recipes);
        SetupConfirmDialog();
        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();
        component.WaitForElement("button[data-action='delete-recipe']");

        component.Find("button[data-action='delete-recipe']").Click();
        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Single(recipes.Deleted);
            Assert.True(component.Find("button[data-action='delete-recipe']").HasAttribute("disabled"));
        });
        component.Find("button[data-action='delete-recipe']").Click();
        Assert.Single(recipes.Deleted);

        pendingDelete.SetResult();
        component.WaitForAssertion(() =>
            Assert.DoesNotContain("Gazpacho", component.Markup, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task Recipes_DeleteRejected_ShowsReferenceConflictAndKeepsRecipe()
    {
        var recipes = new StubRecipesApiClient
        {
            Recipes = [new(RecipeId, "Gazpacho", 20)],
            DeleteException = await CreatePersistenceConflictAsync(),
        };
        Services.AddSingleton<IRecipesApiClient>(recipes);
        SetupConfirmDialog();
        var component = Render<global::Friggy.Web.Components.Pages.Recipes>();
        component.WaitForElement("button[data-action='delete-recipe']");

        component.Find("button[data-action='delete-recipe']").Click();
        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains(
                "asignada a un plan diario",
                component.Find("[role='alert']").TextContent,
                StringComparison.Ordinal);
            Assert.Contains("Gazpacho", component.Markup, StringComparison.Ordinal);
            Assert.False(component.Find("button[data-action='delete-recipe']").HasAttribute("disabled"));
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
            Assert.Contains("Gramo", component.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("(g)", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Vegano", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Comida", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Triturar", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Ingredientes", component.FindAll("h3").Select(item => item.TextContent.Trim()));
            Assert.Contains(
                "1,50 Gramo de Tomate",
                component.Find("[data-testid='step-associated-ingredients']").TextContent,
                StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeDetails_Classification_RendersImmediatelyBelowTitle()
    {
        RegisterApis(new StubRecipesApiClient { Recipe = CompleteRecipe() });

        var component = Render<global::Friggy.Web.Components.Pages.RecipeDetails>(parameters =>
            parameters.Add(page => page.Id, RecipeId));

        component.WaitForAssertion(() =>
        {
            var title = component.Find(".page-header__copy h1");
            var badges = component.Find(".friggy-recipe-details__badges");
            Assert.Equal(title.ParentElement, badges.ParentElement);
            Assert.Equal(badges, title.NextElementSibling);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeDetails_StepWithoutAssociations_RendersExplicitEmptyState()
    {
        var recipes = new StubRecipesApiClient { Recipe = CompleteRecipe(associateIngredient: false) };
        RegisterApis(recipes);

        var component = Render<global::Friggy.Web.Components.Pages.RecipeDetails>(parameters =>
            parameters.Add(page => page.Id, RecipeId));

        component.WaitForAssertion(() =>
            Assert.Equal(
                "Sin ingredientes asociados.",
                component.Find("[data-testid='step-associated-ingredients-empty']")
                    .TextContent
                    .Trim()));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeDetails_RepeatedIngredientLines_RendersEachAssociatedUnit()
    {
        var secondLineId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        var response = CompleteRecipe() with
        {
            Ingredients =
            [
                new(RecipeIngredientId, IngredientId, UnitTypeId, 1.5m, 0),
                new(secondLineId, IngredientId, SecondUnitTypeId, 2m, 1),
            ],
            Steps = [new(Guid.NewGuid(), "Triturar", 5, 0, [RecipeIngredientId, secondLineId])],
        };
        RegisterApis(new StubRecipesApiClient { Recipe = response });

        var component = Render<global::Friggy.Web.Components.Pages.RecipeDetails>(parameters =>
            parameters.Add(page => page.Id, RecipeId));

        component.WaitForAssertion(() =>
        {
            var associations = component.Find("[data-testid='step-associated-ingredients']");
            Assert.Contains("1,50 Gramo de Tomate", associations.TextContent, StringComparison.Ordinal);
            Assert.Contains("2,00 Unidad de Tomate", associations.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("(g)", associations.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("(ud)", associations.TextContent, StringComparison.Ordinal);
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
        component.Find("[data-testid='step-ingredients'] input[type='checkbox']").Change(true);
        component.Find($"#tag-{TagId}").Change(true);
        component.Find($"#meal-type-{MealTypeId}").Change(true);
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var request = Assert.Single(recipes.Created);
            Assert.Equal("Gazpacho", request.Name);
            var ingredient = Assert.Single(request.Ingredients);
            Assert.Equal(1.5m, ingredient.Quantity);
            Assert.NotNull(ingredient.Id);
            var step = Assert.Single(request.Steps);
            Assert.Equal("Triturar", step.Description);
            Assert.Equal([ingredient.Id.Value], step.RecipeIngredientIds);
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
        component.Find("button[aria-label='Editar paso 1']").Click();
        Assert.True(
            component.Find($"input[data-recipe-ingredient-id='{RecipeIngredientId}']")
                .HasAttribute("checked"));
        component.Find("#recipe-name").Change("Salmorejo");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            var update = Assert.Single(recipes.Updated);
            Assert.Equal(RecipeId, update.Id);
            Assert.Equal("Salmorejo", update.Request.Name);
            var ingredient = Assert.Single(update.Request.Ingredients);
            Assert.Equal(1.5m, ingredient.Quantity);
            Assert.Equal(RecipeIngredientId, ingredient.Id);
            Assert.Equal(
                [RecipeIngredientId],
                Assert.Single(update.Request.Steps).RecipeIngredientIds);
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

    private void SetupConfirmDialog()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
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

    private static string[] VisibleRecipeNames(
        IRenderedComponent<global::Friggy.Web.Components.Pages.Recipes> component) =>
        component.FindAll(".friggy-recipe-card h3")
            .Select(item => item.TextContent.Trim())
            .ToArray();

    private static RecipeResponse CompleteRecipe(bool associateIngredient = true) =>
        new(
            RecipeId,
            "Gazpacho",
            20,
            [new(RecipeIngredientId, IngredientId, UnitTypeId, 1.5m, 0)],
            [
                new(
                    Guid.NewGuid(),
                    "Triturar",
                    5,
                    0,
                    associateIngredient ? [RecipeIngredientId] : []),
            ],
            [TagId],
            [MealTypeId]);

    private static async Task<ApiProblemException> CreatePersistenceConflictAsync()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(new ProblemDetails
            {
                Status = (int)HttpStatusCode.Conflict,
                Detail = "La operación entra en conflicto con el estado actual de los datos.",
                Extensions = { ["code"] = "persistence.conflict" },
            }),
        };

        return await ApiProblemException.FromResponseAsync(
            response,
            Xunit.TestContext.Current.CancellationToken);
    }

    private sealed class StubRecipesApiClient : IRecipesApiClient
    {
        public IReadOnlyList<RecipeListItemResponse> Recipes { get; init; } = [];
        public RecipeResponse? Recipe { get; init; }
        public TaskCompletionSource<IReadOnlyList<RecipeListItemResponse>>? PendingList { get; init; }
        public TaskCompletionSource? PendingDelete { get; init; }
        public Exception? ListException { get; init; }
        public ApiProblemException? SaveException { get; init; }
        public ApiProblemException? DeleteException { get; init; }
        public List<CreateRecipeRequest> Created { get; } = [];
        public List<(Guid Id, UpdateRecipeRequest Request)> Updated { get; } = [];
        public List<Guid> Deleted { get; } = [];

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

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Deleted.Add(id);

            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            return PendingDelete?.Task ?? Task.CompletedTask;
        }
    }

    private sealed class IngredientsApiClientStub : IIngredientsApiClient
    {
        public Exception? ListException { get; init; }
        public IReadOnlyList<IngredientResponse> Items { get; init; } = [new(IngredientId, "Tomate")];

        public Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }

            return Task.FromResult(Items);
        }
        public Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class UnitTypesApiClientStub : IUnitTypesApiClient
    {
        public Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnitTypeResponse>>(
                [new(UnitTypeId, "Gramo", "g"), new(SecondUnitTypeId, "Unidad", "ud")]);
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
        public IReadOnlyList<MealTypeResponse> Items { get; init; } = [new(MealTypeId, "Comida", 1)];
        public Task<IReadOnlyList<MealTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Items);
        public Task<MealTypeResponse> CreateAsync(CreateMealTypeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MealTypeResponse> UpdateAsync(Guid id, UpdateMealTypeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
