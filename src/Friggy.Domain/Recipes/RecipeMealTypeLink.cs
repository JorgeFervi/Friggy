namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="RecipeMealTypeLink"/> que representa la asociación entre
/// una receta y un tipo de comida.
/// </summary>
public sealed class RecipeMealTypeLink
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private RecipeMealTypeLink()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta asociada.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida asociado.
    /// </param>
    private RecipeMealTypeLink(Guid recipeId, Guid mealTypeId)
    {
        RecipeId = recipeId;
        MealTypeId = mealTypeId;
    }

    /// <summary>
    /// Código de la receta asociada.
    /// </summary>
    public Guid RecipeId { get; private set; }

    /// <summary>
    /// Código del tipo de comida asociado.
    /// </summary>
    public Guid MealTypeId { get; private set; }

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta asociada.
    /// </param>
    /// <param name="mealTypeId">
    /// Código del tipo de comida asociado.
    /// </param>
    /// <returns>
    /// Objeto <see cref="RecipeMealTypeLink"/>.
    /// </returns>
    internal static RecipeMealTypeLink Create(Guid recipeId, Guid mealTypeId) =>
        new(recipeId, mealTypeId);
}
