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
            Assert.Equal(detail, component.Find("[role='alert']").TextContent);
            Assert.Equal("Tomate", component.Find("#ingredient-name").GetAttribute("value"));
        });
        Assert.Empty(api.Created);
    }

    private sealed class StubIngredientsApiClient : IIngredientsApiClient
    {
        public Exception? ListException { get; init; }
        public ApiProblemException? CreateException { get; init; }
        public List<string> Created { get; } = [];
        public Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }

            return Task.FromResult<IReadOnlyList<IngredientResponse>>(
                Created.Select(name => new IngredientResponse(Guid.NewGuid(), name)).ToArray());
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
