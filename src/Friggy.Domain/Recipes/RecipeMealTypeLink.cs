namespace Friggy.Domain.Recipes;

public sealed class RecipeMealTypeLink
{
    private RecipeMealTypeLink()
    {
    }

    private RecipeMealTypeLink(Guid recipeId, Guid mealTypeId)
    {
        RecipeId = recipeId;
        MealTypeId = mealTypeId;
    }

    public Guid RecipeId { get; private set; }

    public Guid MealTypeId { get; private set; }

    internal static RecipeMealTypeLink Create(Guid recipeId, Guid mealTypeId) =>
        new(recipeId, mealTypeId);
}
