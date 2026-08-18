using Bunit;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Catalogs;

public sealed class CatalogPagesTests : ComponentTest
{
    [Theory]
    [InlineData("ingredients", "Tomate", "#ingredient-name")]
    [InlineData("unit-types", "Gramo", "#unit-name")]
    [InlineData("recipe-tags", "Vegano", "#tag-name")]
    [InlineData("meal-types", "Cena", "#meal-name")]
    [Trait("Category", "Component")]
    public void Catalog_PopulatedState_ExposesEquivalentResponsiveActionsAndEditState(
        string catalog,
        string itemName,
        string inputSelector)
    {
        var component = RenderCatalog(catalog, populated: true);

        component.WaitForAssertion(() =>
        {
            Assert.NotNull(component.Find(".page-header"));
            Assert.NotNull(component.Find(".friggy-catalog-page__cards"));
            Assert.NotNull(component.Find(".friggy-catalog-page__table"));
            Assert.Equal(2, component.FindAll($"button[aria-label='Editar {itemName}']").Count);
            Assert.Equal(2, component.FindAll($"button[aria-label='Borrar {itemName}']").Count);
        });

        component.Find($"button[aria-label='Editar {itemName}']").Click();

        Assert.Equal(itemName, component.Find(inputSelector).GetAttribute("value"));
        Assert.Contains("Guardar cambios", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Cancelar edición", component.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ingredients", "No hay ingredientes")]
    [InlineData("unit-types", "No hay unidades")]
    [InlineData("recipe-tags", "No hay etiquetas")]
    [InlineData("meal-types", "No hay tipos de comida")]
    [Trait("Category", "Component")]
    public void Catalog_EmptyState_UsesFeedbackPanel(string catalog, string expectedText)
    {
        var component = RenderCatalog(catalog, populated: false);

        component.WaitForAssertion(() =>
        {
            Assert.Contains(expectedText, component.Markup, StringComparison.Ordinal);
            Assert.NotNull(component.Find(".feedback-panel"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void UnitTypes_ApiReturnsUnit_RendersNameAndSymbol()
    {
        Api.RespondWith("application/json", "[{\"id\":\"10000000-0000-0000-0000-000000000001\",\"name\":\"Gramo\",\"symbol\":\"g\"}]");
        Services.AddSingleton<IUnitTypesApiClient>(new CatalogApiClient(ApiClient));

        var component = Render<global::Friggy.Web.Components.Pages.UnitTypes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Gramo", component.Markup, StringComparison.Ordinal);
            Assert.Contains(">g<", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeTags_ApiReturnsEmpty_ShowsEmptyState()
    {
        Api.RespondWith("application/json", "[]");
        Services.AddSingleton<IRecipeTagsApiClient>(new CatalogApiClient(ApiClient));

        var component = Render<global::Friggy.Web.Components.Pages.RecipeTags>();

        component.WaitForAssertion(() =>
            Assert.Contains("No hay etiquetas", component.Markup, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MealTypes_ApiReturnsMealType_RendersOrderedValue()
    {
        Api.RespondWith("application/json", "[{\"id\":\"20000000-0000-0000-0000-000000000003\",\"name\":\"Cena\",\"order\":2}]");
        Services.AddSingleton<IMealTypesApiClient>(new CatalogApiClient(ApiClient));

        var component = Render<global::Friggy.Web.Components.Pages.MealTypes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Cena", component.Markup, StringComparison.Ordinal);
            Assert.Contains(">2<", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void UnitTypes_ApiUnavailableWhileLoading_ShowsContextWithoutEmptyState()
    {
        var api = new StubUnitTypesApiClient
        {
            ListException = new HttpRequestException("Conexión rechazada."),
        };
        Services.AddSingleton<IUnitTypesApiClient>(api);

        var component = Render<global::Friggy.Web.Components.Pages.UnitTypes>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains(
                "No se pudieron cargar las unidades",
                component.Find("[role='alert']").TextContent,
                StringComparison.Ordinal);
            Assert.DoesNotContain("No hay unidades", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void UnitTypes_SubmitApiUnavailable_ShowsErrorAndKeepsFormData()
    {
        var api = new StubUnitTypesApiClient
        {
            CreateException = new HttpRequestException("Conexión rechazada."),
        };
        Services.AddSingleton<IUnitTypesApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.UnitTypes>();
        component.WaitForElement("form");

        component.Find("#unit-name").Change("Taza");
        component.Find("#unit-symbol").Change("tza");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Conexión rechazada.", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal("Taza", component.Find("#unit-name").GetAttribute("value"));
            Assert.Equal("tza", component.Find("#unit-symbol").GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeTags_SubmitApiUnavailable_ShowsErrorAndKeepsFormData()
    {
        var api = new StubRecipeTagsApiClient
        {
            CreateException = new HttpRequestException("Conexión rechazada."),
        };
        Services.AddSingleton<IRecipeTagsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.RecipeTags>();
        component.WaitForElement("form");

        component.Find("#tag-name").Change("Vegano");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Conexión rechazada.", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal("Vegano", component.Find("#tag-name").GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MealTypes_SubmitApiUnavailable_ShowsErrorAndKeepsFormData()
    {
        var api = new StubMealTypesApiClient
        {
            CreateException = new HttpRequestException("Conexión rechazada."),
        };
        Services.AddSingleton<IMealTypesApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.MealTypes>();
        component.WaitForElement("form");

        component.Find("#meal-name").Change("Merienda");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Conexión rechazada.", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal("Merienda", component.Find("#meal-name").GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void UnitTypes_DeleteApiUnavailable_ShowsError()
    {
        var unit = new UnitTypeResponse(Guid.NewGuid(), "Taza", "tza");
        var api = new StubUnitTypesApiClient
        {
            Items = [unit],
            DeleteException = new HttpRequestException("Conexión rechazada."),
        };
        Services.AddSingleton<IUnitTypesApiClient>(api);
        SetupConfirmDialog();
        var component = Render<global::Friggy.Web.Components.Pages.UnitTypes>();
        component.WaitForElement("table");

        component.Find("button[aria-label='Borrar Taza']").Click();
        component.FindAll("button").Single(button => button.TextContent.Contains("Borrar unidad", StringComparison.Ordinal)).Click();

        component.WaitForAssertion(() =>
            Assert.Contains("Conexión rechazada.", component.Find("[role='alert']").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void UnitTypes_Dispose_CancelsLifecycleToken()
    {
        var api = new StubUnitTypesApiClient();

        AssertDisposalCancels<global::Friggy.Web.Components.Pages.UnitTypes, IUnitTypesApiClient>(
            api,
            () => api.ListCancellationToken);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeTags_Dispose_CancelsLifecycleToken()
    {
        var api = new StubRecipeTagsApiClient();

        AssertDisposalCancels<global::Friggy.Web.Components.Pages.RecipeTags, IRecipeTagsApiClient>(
            api,
            () => api.ListCancellationToken);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MealTypes_Dispose_CancelsLifecycleToken()
    {
        var api = new StubMealTypesApiClient();

        AssertDisposalCancels<global::Friggy.Web.Components.Pages.MealTypes, IMealTypesApiClient>(
            api,
            () => api.ListCancellationToken);
    }

    private void AssertDisposalCancels<TComponent, TService>(
        TService api,
        Func<CancellationToken> getToken)
        where TComponent : IComponent
        where TService : class
    {
        Services.AddSingleton(api);
        var component = Render<TComponent>();

        component.WaitForAssertion(() => Assert.True(getToken().CanBeCanceled));
        var disposable = Assert.IsAssignableFrom<IDisposable>(component.Instance);
        disposable.Dispose();

        Assert.True(getToken().IsCancellationRequested);
    }

    private void SetupConfirmDialog()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
    }

    private IRenderedComponent<IComponent> RenderCatalog(string catalog, bool populated)
    {
        const string id = "10000000-0000-0000-0000-000000000001";
        var json = (catalog, populated) switch
        {
            (_, false) => "[]",
            ("ingredients", true) => $"[{{\"id\":\"{id}\",\"name\":\"Tomate\"}}]",
            ("unit-types", true) => $"[{{\"id\":\"{id}\",\"name\":\"Gramo\",\"symbol\":\"g\"}}]",
            ("recipe-tags", true) => $"[{{\"id\":\"{id}\",\"name\":\"Vegano\"}}]",
            ("meal-types", true) => $"[{{\"id\":\"{id}\",\"name\":\"Cena\",\"order\":2}}]",
            _ => throw new ArgumentOutOfRangeException(nameof(catalog)),
        };
        Api.RespondWith("application/json", json);
        var client = new CatalogApiClient(ApiClient);

        return catalog switch
        {
            "ingredients" => RenderIngredients(client),
            "unit-types" => RenderUnitTypes(client),
            "recipe-tags" => RenderRecipeTags(client),
            "meal-types" => RenderMealTypes(client),
            _ => throw new ArgumentOutOfRangeException(nameof(catalog)),
        };
    }

    private IRenderedComponent<IComponent> RenderIngredients(CatalogApiClient client)
    {
        Services.AddSingleton<IIngredientsApiClient>(client);
        return Render<global::Friggy.Web.Components.Pages.Ingredients>();
    }

    private IRenderedComponent<IComponent> RenderUnitTypes(CatalogApiClient client)
    {
        Services.AddSingleton<IUnitTypesApiClient>(client);
        return Render<global::Friggy.Web.Components.Pages.UnitTypes>();
    }

    private IRenderedComponent<IComponent> RenderRecipeTags(CatalogApiClient client)
    {
        Services.AddSingleton<IRecipeTagsApiClient>(client);
        return Render<global::Friggy.Web.Components.Pages.RecipeTags>();
    }

    private IRenderedComponent<IComponent> RenderMealTypes(CatalogApiClient client)
    {
        Services.AddSingleton<IMealTypesApiClient>(client);
        return Render<global::Friggy.Web.Components.Pages.MealTypes>();
    }

    private sealed class StubUnitTypesApiClient : IUnitTypesApiClient
    {
        public IReadOnlyList<UnitTypeResponse> Items { get; init; } = [];
        public Exception? ListException { get; init; }
        public Exception? CreateException { get; init; }
        public Exception? DeleteException { get; init; }
        public CancellationToken ListCancellationToken { get; private set; }

        public Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken)
        {
            ListCancellationToken = cancellationToken;
            return ListException is null
                ? Task.FromResult(Items)
                : Task.FromException<IReadOnlyList<UnitTypeResponse>>(ListException);
        }

        public Task<UnitTypeResponse> CreateAsync(
            CreateUnitTypeRequest request,
            CancellationToken cancellationToken) =>
            CreateException is null
                ? Task.FromResult(new UnitTypeResponse(Guid.NewGuid(), request.Name, request.Symbol))
                : Task.FromException<UnitTypeResponse>(CreateException);

        public Task<UnitTypeResponse> UpdateAsync(
            Guid id,
            UpdateUnitTypeRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new UnitTypeResponse(id, request.Name, request.Symbol));

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            DeleteException is null
                ? Task.CompletedTask
                : Task.FromException(DeleteException);
    }

    private sealed class StubRecipeTagsApiClient : IRecipeTagsApiClient
    {
        public Exception? CreateException { get; init; }
        public CancellationToken ListCancellationToken { get; private set; }

        public Task<IReadOnlyList<RecipeTagResponse>> ListAsync(CancellationToken cancellationToken)
        {
            ListCancellationToken = cancellationToken;
            return Task.FromResult<IReadOnlyList<RecipeTagResponse>>([]);
        }

        public Task<RecipeTagResponse> CreateAsync(
            CreateRecipeTagRequest request,
            CancellationToken cancellationToken) =>
            CreateException is null
                ? Task.FromResult(new RecipeTagResponse(Guid.NewGuid(), request.Name))
                : Task.FromException<RecipeTagResponse>(CreateException);

        public Task<RecipeTagResponse> UpdateAsync(
            Guid id,
            UpdateRecipeTagRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new RecipeTagResponse(id, request.Name));

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubMealTypesApiClient : IMealTypesApiClient
    {
        public Exception? CreateException { get; init; }
        public CancellationToken ListCancellationToken { get; private set; }

        public Task<IReadOnlyList<MealTypeResponse>> ListAsync(CancellationToken cancellationToken)
        {
            ListCancellationToken = cancellationToken;
            return Task.FromResult<IReadOnlyList<MealTypeResponse>>([]);
        }

        public Task<MealTypeResponse> CreateAsync(
            CreateMealTypeRequest request,
            CancellationToken cancellationToken) =>
            CreateException is null
                ? Task.FromResult(new MealTypeResponse(Guid.NewGuid(), request.Name, request.Order))
                : Task.FromException<MealTypeResponse>(CreateException);

        public Task<MealTypeResponse> UpdateAsync(
            Guid id,
            UpdateMealTypeRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MealTypeResponse(id, request.Name, request.Order));

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
