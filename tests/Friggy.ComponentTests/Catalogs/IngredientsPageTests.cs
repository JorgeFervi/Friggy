using System.Net;
using System.Net.Http.Json;
using Bunit;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Catalogs;

public sealed class IngredientsPageTests : ComponentTest
{
    private const int PageSize = 10;

    [Fact]
    [Trait("Category", "Component")]
    public void Render_EmptyCatalog_ShowsEmptyState()
    {
        Services.AddSingleton<IIngredientsApiClient>(new StubIngredientsApiClient());

        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();

        component.WaitForAssertion(() =>
            Assert.Contains("No hay ingredientes", component.Markup, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Render_ApiUnavailable_ShowsErrorWithoutEmptyState()
    {
        Services.AddSingleton<IIngredientsApiClient>(new StubIngredientsApiClient
        {
            ListException = new HttpRequestException("Conexión rechazada."),
        });

        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("No se pudieron cargar", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("No hay ingredientes", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Submit_EmptyName_ShowsValidationAndDoesNotCallApi()
    {
        var api = new StubIngredientsApiClient();
        Services.AddSingleton<IIngredientsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();
        component.WaitForElement("form");

        component.Find("form").Submit();

        component.WaitForAssertion(() =>
            Assert.Contains("obligatorio", component.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(api.Created);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Submit_ValidName_CreatesIngredientAndRefreshesList()
    {
        var api = new StubIngredientsApiClient();
        Services.AddSingleton<IIngredientsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();
        component.WaitForElement("form");

        component.Find("#ingredient-name").Change("Tomate");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Tomate", api.Created);
            Assert.Contains("Tomate", component.Markup, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "El nombre no es válido.")]
    [InlineData(HttpStatusCode.NotFound, "No se encontró el ingrediente.")]
    [InlineData(HttpStatusCode.Conflict, "Ya existe un ingrediente con ese nombre.")]
    [Trait("Category", "Component")]
    public async Task Submit_ApiReturnsProblem_ShowsDetailAndKeepsFormData(
        HttpStatusCode statusCode,
        string detail)
    {
        using var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(new ProblemDetails
            {
                Status = (int)statusCode,
                Title = "No se pudo guardar el ingrediente.",
                Detail = detail,
            }),
        };
        var api = new StubIngredientsApiClient
        {
            CreateException = await ApiProblemException.FromResponseAsync(
                response,
                Xunit.TestContext.Current.CancellationToken),
        };
        Services.AddSingleton<IIngredientsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();
        component.WaitForElement("form");

        component.Find("#ingredient-name").Change("Tomate");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains(detail, component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal("Tomate", component.Find("#ingredient-name").GetAttribute("value"));
        });
        Assert.Empty(api.Created);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Submit_ApiUnavailable_ShowsDetailAndKeepsFormData()
    {
        var api = new StubIngredientsApiClient
        {
            CreateException = new HttpRequestException("Conexión rechazada."),
        };
        Services.AddSingleton<IIngredientsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();
        component.WaitForElement("form");

        component.Find("#ingredient-name").Change("Tomate");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Conexión rechazada.", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal("Tomate", component.Find("#ingredient-name").GetAttribute("value"));
        });
        Assert.Empty(api.Created);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Render_MoreThanTenIngredients_ShowsTenRowsAndPaginationStatus()
    {
        var api = new StubIngredientsApiClient
        {
            Items = CreateIngredients(PageSize + 2),
        };
        Services.AddSingleton<IIngredientsApiClient>(api);

        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(PageSize, component.FindAll(".friggy-catalog-page__table tbody tr").Count);
            Assert.Contains(
                "Mostrando 1-10 de 12 ingredientes",
                component.Find("[data-testid='ingredient-result-status']").TextContent,
                StringComparison.Ordinal);
            Assert.Contains(
                "Página 1 de 2",
                component.Find("[data-testid='ingredient-page-status']").TextContent,
                StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Pagination_NextPage_ShowsRemainingIngredientsAndUpdatesNavigation()
    {
        var api = new StubIngredientsApiClient
        {
            Items = CreateIngredients(PageSize + 2),
        };
        Services.AddSingleton<IIngredientsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();
        component.WaitForElement("button[aria-label='Página siguiente']");

        component.Find("button[aria-label='Página siguiente']").Click();

        Assert.Equal(2, component.FindAll(".friggy-catalog-page__table tbody tr").Count);
        Assert.Contains(
            "Mostrando 11-12 de 12 ingredientes",
            component.Find("[data-testid='ingredient-result-status']").TextContent,
            StringComparison.Ordinal);
        Assert.Contains(
            "Página 2 de 2",
            component.Find("[data-testid='ingredient-page-status']").TextContent,
            StringComparison.Ordinal);
        Assert.NotNull(component.Find("button[aria-label='Página siguiente'][disabled]"));
        Assert.NotNull(component.Find("button[aria-label='Página anterior']:not([disabled])"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Search_ByName_FiltersEveryPageAndReturnsToFirstPage()
    {
        var api = new StubIngredientsApiClient
        {
            Items = [.. CreateIngredients(PageSize + 1), new(Guid.NewGuid(), "Tomate cherry")],
        };
        Services.AddSingleton<IIngredientsApiClient>(api);
        var component = Render<global::Friggy.Web.Components.Pages.Ingredients>();
        component.WaitForElement("button[aria-label='Página siguiente']");
        component.Find("button[aria-label='Página siguiente']").Click();

        component.Find("#ingredient-search").Input("tomate");

        var rows = component.FindAll(".friggy-catalog-page__table tbody tr");
        Assert.Single(rows);
        Assert.Contains("Tomate cherry", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains(
            "Mostrando 1 de 1 ingrediente",
            component.Find("[data-testid='ingredient-result-status']").TextContent,
            StringComparison.Ordinal);
        Assert.Empty(component.FindAll("[data-testid='ingredient-pagination']"));
    }

    private static IngredientResponse[] CreateIngredients(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new IngredientResponse(Guid.NewGuid(), $"Ingrediente {index:00}"))
            .ToArray();

    private sealed class StubIngredientsApiClient : IIngredientsApiClient
    {
        public IReadOnlyList<IngredientResponse> Items { get; init; } = [];
        public Exception? ListException { get; init; }
        public Exception? CreateException { get; init; }
        public List<string> Created { get; } = [];
        public Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }

            return Task.FromResult<IReadOnlyList<IngredientResponse>>(
                [.. Items, .. Created.Select(name => new IngredientResponse(Guid.NewGuid(), name))]);
        }
        public Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            Created.Add(request.Name);
            return Task.FromResult(new IngredientResponse(Guid.NewGuid(), request.Name));
        }
        public Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
