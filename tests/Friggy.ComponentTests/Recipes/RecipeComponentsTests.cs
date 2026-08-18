using Bunit;
using Friggy.Application.Recipes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Components.Recipes;

namespace Friggy.ComponentTests.Recipes;

public sealed class RecipeComponentsTests : ComponentTest
{
    private static readonly RecipeListItemResponse Recipe = new(
        Guid.Parse("50000000-0000-0000-0000-000000000001"),
        "Gazpacho",
        20);

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeCard_ReadOnlyContext_ShowsDetailAndNoManagementActions()
    {
        var component = Render<RecipeCard>(parameters => parameters
            .Add(card => card.Recipe, Recipe));

        var card = component.Find("article");
        Assert.Equal(Recipe.Id.ToString(), card.GetAttribute("data-recipe-id"));
        Assert.Equal("Gazpacho", card.QuerySelector("h3")?.TextContent.Trim());
        Assert.Contains("20 min", card.TextContent, StringComparison.Ordinal);
        Assert.Equal($"recipes/{Recipe.Id}", card.QuerySelector("a[data-testid='recipe-detail']")?.GetAttribute("href"));
        Assert.Empty(card.QuerySelectorAll("button"));
        Assert.Null(card.QuerySelector("a[href$='/edit']"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeCard_ManagementContext_ExposesEditAndSingleDeleteRequest()
    {
        var requested = new List<RecipeListItemResponse>();
        var component = Render<RecipeCard>(parameters => parameters
            .Add(card => card.Recipe, Recipe)
            .Add(card => card.ShowManagementActions, true)
            .Add(card => card.OnDeleteRequested, recipe => requested.Add(recipe)));

        Assert.Equal($"recipes/{Recipe.Id}/edit", component.Find("a[href$='/edit']").GetAttribute("href"));
        component.Find("button[data-action='delete-recipe']").Click();

        Assert.Equal(Recipe, Assert.Single(requested));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeCard_DeletingState_DisablesDestructiveAction()
    {
        var component = Render<RecipeCard>(parameters => parameters
            .Add(card => card.Recipe, Recipe)
            .Add(card => card.ShowManagementActions, true)
            .Add(card => card.Deleting, true));

        var delete = component.Find("button[data-action='delete-recipe']");
        Assert.True(delete.HasAttribute("disabled"));
        Assert.Contains("Borrando", delete.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RecipeGrid_MultipleRecipes_RendersStableListOfCards()
    {
        var second = Recipe with
        {
            Id = Guid.Parse("50000000-0000-0000-0000-000000000002"),
            Name = "Salmorejo",
        };
        var component = Render<RecipeGrid>(parameters => parameters
            .Add(grid => grid.Recipes, [Recipe, second]));

        Assert.Equal("Lista de recetas", component.Find("ul").GetAttribute("aria-label"));
        Assert.Equal(2, component.FindAll("li > article").Count);
        Assert.Equal([Recipe.Id.ToString(), second.Id.ToString()],
            component.FindAll("article").Select(card => card.GetAttribute("data-recipe-id")));
    }
}
