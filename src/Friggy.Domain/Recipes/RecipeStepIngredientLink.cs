using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="RecipeStepIngredientLink"/> que representa la asociación
/// entre un paso y una línea de ingrediente de la misma receta.
/// </summary>
public sealed class RecipeStepIngredientLink
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private RecipeStepIngredientLink()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta asociada.
    /// </param>
    /// <param name="recipeStepId">
    /// Código del paso asociado.
    /// </param>
    /// <param name="recipeIngredientId">
    /// Código de la línea de ingrediente asociada.
    /// </param>
    private RecipeStepIngredientLink(
        Guid recipeId,
        Guid recipeStepId,
        Guid recipeIngredientId)
    {
        RecipeId = recipeId;
        RecipeStepId = recipeStepId;
        RecipeIngredientId = recipeIngredientId;
    }

    /// <summary>
    /// Código de la receta asociada.
    /// </summary>
    public Guid RecipeId { get; private set; }

    /// <summary>
    /// Código del paso asociado.
    /// </summary>
    public Guid RecipeStepId { get; private set; }

    /// <summary>
    /// Código de la línea de ingrediente asociada.
    /// </summary>
    public Guid RecipeIngredientId { get; private set; }

    /// <summary>
    /// Constructor interno principal que crea una asociación entre un paso y
    /// una línea de ingrediente.
    /// </summary>
    /// <param name="step">
    /// Paso que se va a asociar.
    /// </param>
    /// <param name="ingredient">
    /// Línea de ingrediente que se va a asociar.
    /// </param>
    /// <returns>
    /// Objeto <see cref="RecipeStepIngredientLink"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando el paso o la línea de ingrediente son nulos.
    /// </exception>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el paso y la línea pertenecen a
    /// recetas distintas.
    /// </exception>
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
