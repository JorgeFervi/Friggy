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
            Assert.Equal("Conexión rechazada.", component.Find("[role='alert']").TextContent);
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
            Assert.Equal("Conexión rechazada.", component.Find("[role='alert']").TextContent);
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
            Assert.Equal("Conexión rechazada.", component.Find("[role='alert']").TextContent);
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
        JavaScript.Setup<bool>("confirm", _ => true).SetResult(true);
        var component = Render<global::Friggy.Web.Components.Pages.UnitTypes>();
        component.WaitForElement("table");

        component.FindAll("button").Single(button => button.TextContent == "Borrar").Click();

        component.WaitForAssertion(() =>
            Assert.Equal("Conexión rechazada.", component.Find("[role='alert']").TextContent));
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
