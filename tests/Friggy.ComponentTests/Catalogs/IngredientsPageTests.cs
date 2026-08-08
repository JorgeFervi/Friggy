using Bunit;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
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

    private sealed class StubIngredientsApiClient : IIngredientsApiClient
    {
        public List<string> Created { get; } = [];
        public Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IngredientResponse>>(
                Created.Select(name => new IngredientResponse(Guid.NewGuid(), name)).ToArray());
        public Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken)
        {
            Created.Add(request.Name);
            return Task.FromResult(new IngredientResponse(Guid.NewGuid(), request.Name));
        }
        public Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
