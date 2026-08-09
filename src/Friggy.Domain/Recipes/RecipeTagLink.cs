namespace Friggy.Domain.Recipes;

public sealed class RecipeTagLink
{
    private RecipeTagLink()
    {
    }

    private RecipeTagLink(Guid recipeId, Guid recipeTagId)
    {
        RecipeId = recipeId;
        RecipeTagId = recipeTagId;
    }

    public Guid RecipeId { get; private set; }

    public Guid RecipeTagId { get; private set; }

    internal static RecipeTagLink Create(Guid recipeId, Guid recipeTagId) =>
        new(recipeId, recipeTagId);
}
