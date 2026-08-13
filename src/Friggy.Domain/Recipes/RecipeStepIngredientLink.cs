using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

public sealed class RecipeStepIngredientLink
{
    private RecipeStepIngredientLink()
    {
    }

    private RecipeStepIngredientLink(
        Guid recipeId,
        Guid recipeStepId,
        Guid recipeIngredientId)
    {
        RecipeId = recipeId;
        RecipeStepId = recipeStepId;
        RecipeIngredientId = recipeIngredientId;
    }

    public Guid RecipeId { get; private set; }

    public Guid RecipeStepId { get; private set; }

    public Guid RecipeIngredientId { get; private set; }

    internal static RecipeStepIngredientLink Create(
        RecipeStep step,
        RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(ingredient);

        if (step.RecipeId != ingredient.RecipeId)
        {
            throw new DomainValidationException(
                "recipe-step.ingredient.recipe-mismatch",
                "El paso y la línea de ingrediente deben pertenecer a la misma receta.");
        }

        return new RecipeStepIngredientLink(
            step.RecipeId,
            step.Id,
            ingredient.Id);
    }
}
