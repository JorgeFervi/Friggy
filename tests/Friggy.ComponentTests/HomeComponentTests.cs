using Bunit;
using Friggy.Application.Recipes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests;

public sealed class HomeComponentTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void Render_PendingRecipes_ShowsEditorialHeaderAndLoadingState()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<RecipeListItemResponse>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton<IRecipesApiClient>(new HomeRecipesApiClient { PendingList = pending });

        var component = Render<global::Friggy.Web.Components.Pages.Home>();

        Assert.Equal("Tu cocina, organizada", component.Find("h1").TextContent.Trim());
        Assert.Equal("recipes/new", component.Find("a[href='recipes/new']").GetAttribute("href"));
        Assert.Equal("Cargando recetas", component.Find("[role='status'] h2").TextContent.Trim());

        pending.SetResult([]);
        component.WaitForAssertion(() =>
            Assert.Equal("Aún no hay recetas", component.Find("[role='status'] h2").TextContent.Trim()));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Render_RecipesAvailable_ShowsCardsFromExistingContract()
    {
        var recipeId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        Services.AddSingleton<IRecipesApiClient>(new HomeRecipesApiClient
        {
            Recipes = [new(recipeId, "Gazpacho", 20)],
        });

        var component = Render<global::Friggy.Web.Components.Pages.Home>();

        component.WaitForAssertion(() =>
        {
            var card = component.Find("article[data-recipe-id]");
            Assert.Contains("Gazpacho", card.TextContent, StringComparison.Ordinal);
            Assert.Contains("20 min", card.TextContent, StringComparison.Ordinal);
            Assert.Equal($"recipes/{recipeId}", card.QuerySelector("a")?.GetAttribute("href"));
            Assert.Empty(card.QuerySelectorAll("button"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Render_RecipesFail_ShowsActionableErrorWithoutEmptyState()
    {
        Services.AddSingleton<IRecipesApiClient>(new HomeRecipesApiClient
        {
            ListException = new HttpRequestException("La API no está disponible."),
        });

        var component = Render<global::Friggy.Web.Components.Pages.Home>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("La API no está disponible", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Aún no hay recetas", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task ApiClient_QueuedResponse_ReturnsConfiguredPayload()
    {
        Api.RespondWith("application/json", "{\"status\":\"ready\"}");

        var payload = await ApiClient.GetStringAsync(
            "/api/status",
            Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("{\"status\":\"ready\"}", payload);
        Assert.Equal(new Uri("http://localhost/api/status"), Api.LastRequest?.RequestUri);
    }

    private sealed class HomeRecipesApiClient : IRecipesApiClient
    {
        public IReadOnlyList<RecipeListItemResponse> Recipes { get; init; } = [];
        public TaskCompletionSource<IReadOnlyList<RecipeListItemResponse>>? PendingList { get; init; }
        public Exception? ListException { get; init; }

        public Task<IReadOnlyList<RecipeListItemResponse>> ListAsync(CancellationToken cancellationToken)
        {
            if (ListException is not null)
            {
                throw ListException;
            }
            return PendingList?.Task ?? Task.FromResult(Recipes);
        }

        public Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RecipeResponse> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
