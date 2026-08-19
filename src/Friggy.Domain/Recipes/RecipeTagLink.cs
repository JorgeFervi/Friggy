namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="RecipeTagLink"/> que representa la asociación entre
/// una receta y una etiqueta.
/// </summary>
public sealed class RecipeTagLink
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private RecipeTagLink()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta asociada.
    /// </param>
    /// <param name="recipeTagId">
    /// Código de la etiqueta asociada.
    /// </param>
    private RecipeTagLink(Guid recipeId, Guid recipeTagId)
    {
        RecipeId = recipeId;
        RecipeTagId = recipeTagId;
    }

    /// <summary>
    /// Código de la receta asociada.
    /// </summary>
    public Guid RecipeId { get; private set; }

    /// <summary>
    /// Código de la etiqueta asociada.
    /// </summary>
    public Guid RecipeTagId { get; private set; }

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta asociada.
    /// </param>
    /// <param name="recipeTagId">
    /// Código de la etiqueta asociada.
    /// </param>
    /// <returns>
    /// Objeto <see cref="RecipeTagLink"/>.
    /// </returns>
    internal static RecipeTagLink Create(Guid recipeId, Guid recipeTagId) =>
        new(recipeId, recipeTagId);
}
